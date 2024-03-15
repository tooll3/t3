#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Compilation;
using T3.Editor.Gui.Graph.Helpers;

namespace T3.Editor.UiModel;


internal sealed partial class EditableSymbolProject : EditorSymbolPackage
{
    public override AssemblyInformation AssemblyInformation => CsProjectFile.Assembly;

    public EditableSymbolProject(CsProjectFile csProjectFile) : base(csProjectFile.Assembly, false)
    {
        CsProjectFile = csProjectFile;
        AllProjectsRw.Add(this);
        InitializeFileWatcher();
        _csFileWatcher = new EditablePackageFsWatcher(this, OnFileChanged, OnFileRenamed);
    }

    public bool TryCreateHome()
    {
        if (!CsProjectFile.Assembly.HasHome)
            return false;

        Log.Debug($"Creating home for {CsProjectFile.Name}...");
        var homeGuid = CsProjectFile.Assembly.HomeGuid;
        var homeSymbol = Symbols[homeGuid];

        RootSymbolUi.AddChild(homeSymbol, Guid.NewGuid(), new Vector2(0, _newProjectPosition), SymbolChildUi.DefaultOpSize, "");
        _newProjectPosition += (int)SymbolChildUi.DefaultOpSize.Y + 20;

        return true;
    }

    public bool TryCompile(string sourceCode, string newSymbolName, Guid newSymbolId, string nameSpace, out Symbol? newSymbol)
    {
        var path = SymbolPathHandler.GetCorrectPath(newSymbolName, nameSpace, Folder, CsProjectFile.RootNamespace, SourceCodeExtension);
        
        try
        {
            File.WriteAllText(path, sourceCode);
        }
        catch
        {
            Log.Error($"Could not write source code to {path}");
            newSymbol = null;
            return false;
        }
        
        if (TryRecompile())
        {
            UpdateSymbols();
            return Symbols.TryGetValue(newSymbolId, out newSymbol);
        }

        newSymbol = null;
        return false;
    }

    // todo : determine name from source code
    public bool TryRecompileWithNewSource(Symbol symbol, string newSource)
    {
        var gotCurrentSource = _filePathHandlers.TryGetValue(symbol.Id, out var currentSourcePath);
        if (!gotCurrentSource || currentSourcePath!.SourceCodePath == null)
        {
            Log.Error($"Could not find original source code for symbol \"{symbol.Name}\"");
            return false;
        }

        string currentSourceCode;

        try
        {
            currentSourceCode = File.ReadAllText(currentSourcePath.SourceCodePath);
        }
        catch
        {
            Log.Error($"Could not read original source code at \"{currentSourcePath}\"");
            return false;
        }

        symbol.PendingSource = newSource;

        var symbolUi = SymbolUis[symbol.Id];
        symbolUi.FlagAsModified();

        if (TryRecompile())
        {
            UpdateSymbols();
            return true;
        }

        symbol.PendingSource = currentSourceCode;
        symbolUi.FlagAsModified();
        SaveModifiedSymbols();

        return false;
    }

    internal bool TryRecompile()
    {
        NeedsCompilation = false;
        
        SaveModifiedSymbols();
        
        MarkAsSaving();
        var updated = CsProjectFile.TryRecompile();
        UnmarkAsSaving();

        if (!updated)
            return false;

        return true;
    }

    public void UpdateSymbols()
    {
        ProjectSetup.UpdateSymbolPackage(this);
    }

    public void ReplaceSymbolUi(SymbolUi symbolUi)
    {
        var symbol = symbolUi.Symbol;
        SymbolUiRegistry.EntriesEditable[symbol.Id] = symbolUi;
        SymbolUis[symbol.Id] = symbolUi;

        Log.Debug($"Replaced symbol ui for {symbol.Name}");
    }

    public void RenameNamespace(string sourceNamespace, string newNamespace, EditableSymbolProject newDestinationProject)
    {
        // copy since we are modifying the collection while iterating
        var mySymbols = Symbols.Values.ToArray();
        foreach (var symbol in mySymbols)
        {
            if (!symbol.Namespace.StartsWith(sourceNamespace))
                continue;

            var substitutedNamespace = Regex.Replace(symbol.Namespace, sourceNamespace, newNamespace);

            ChangeNamespaceOf(symbol.Id, substitutedNamespace, newDestinationProject, sourceNamespace);
        }
    }

    public void ChangeNamespaceOf(Guid id, string newNamespace, EditableSymbolProject newDestinationProject, string? sourceNamespace = null)
    {
        var symbol = Symbols[id];
        sourceNamespace ??= symbol.Namespace;
        if (_filePathHandlers.TryGetValue(id, out var filePathHandler) && filePathHandler.SourceCodePath != null)
        {
            if (!TryConvertToValidCodeNamespace(sourceNamespace, out var sourceCodeNamespace))
            {
                Log.Error($"Source namespace {sourceNamespace} is not a valid namespace. This is a bug.");
                return;
            }

            if (!TryConvertToValidCodeNamespace(newNamespace, out var newCodeNamespace))
            {
                Log.Error($"{newNamespace} is not a valid namespace.");
                return;
            }

            var sourceCode = File.ReadAllText(filePathHandler.SourceCodePath);
            var newSourceCode = Regex.Replace(sourceCode, sourceCodeNamespace, newCodeNamespace);
            symbol.PendingSource = newSourceCode;
        }
        else
        {
            throw new Exception($"Could not find source code for {symbol.Name} in {CsProjectFile.Name} ({id})");
        }

        var symbolUi = SymbolUis[id];
        symbolUi.FlagAsModified();

        if (newDestinationProject != this)
        {
            GiveSymbolToPackage(id, newDestinationProject);
            newDestinationProject.MarkAsNeedingRecompilation();
        }

        MarkAsNeedingRecompilation();
    }

    private static bool TryConvertToValidCodeNamespace(string sourceNamespace, out string result)
    {
        // prepend any reserved words with a '@'
        var parts = sourceNamespace.Split('.');
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (!GraphUtils.IsIdentifierValid(part))
            {
                var newPart = "@" + part;
                if (!GraphUtils.IsIdentifierValid(newPart))
                {
                    result = string.Empty;
                    return false;
                }

                parts[i] = newPart;
            }
        }

        result = string.Join('.', parts);
        return true;
    }

    private void GiveSymbolToPackage(Guid id, EditableSymbolProject newDestinationProject)
    {
        Symbols.Remove(id, out var symbol);
        SymbolUis.Remove(id, out var symbolUi);
        _filePathHandlers.Remove(id, out var symbolPathHandler);
        
        Debug.Assert(symbol != null);
        Debug.Assert(symbolUi != null);
        Debug.Assert(symbolPathHandler != null);

        symbol.SymbolPackage = newDestinationProject;

        newDestinationProject.Symbols.Add(id, symbol);
        newDestinationProject.SymbolUis.TryAdd(id, symbolUi);

        newDestinationProject._filePathHandlers.TryAdd(id, symbolPathHandler);

        symbolUi.FlagAsModified();
        newDestinationProject.MarkAsNeedingRecompilation();
        MarkAsNeedingRecompilation();
    }

    private void MarkAsNeedingRecompilation()
    {
        if (IsSaving)
            return;
        NeedsCompilation = true;
    }

    public override string Folder => CsProjectFile.Directory;

    private string ExcludeFolder => Path.Combine(Folder, "bin");

    protected override IEnumerable<string> SymbolUiSearchFiles
    {
        get
        {
            return Directory.EnumerateDirectories(Folder)
                            .Where(x => !x.StartsWith(ExcludeFolder))
                            .SelectMany(subDir => Directory.EnumerateFiles(subDir, $"*{SymbolUiExtension}", SearchOption.AllDirectories))
                            .Concat(Directory.EnumerateFiles(Folder, $"*{SymbolUiExtension}"));
        }
    }

    protected override IEnumerable<string> SymbolSearchFiles
    {
        get
        {
            return Directory.EnumerateDirectories(Folder)
                            .Where(x => !x.StartsWith(ExcludeFolder))
                            .SelectMany(x => Directory.EnumerateFiles(x, $"*{SymbolExtension}", SearchOption.AllDirectories))
                            .Concat(Directory.EnumerateFiles(Folder, $"*{SymbolExtension}"));
        }
    }

    public readonly CsProjectFile CsProjectFile;

    private static readonly List<EditableSymbolProject> AllProjectsRw = new();
    public static readonly IReadOnlyList<EditableSymbolProject> AllProjects = AllProjectsRw;

    public override bool IsModifiable => true;

    private readonly EditablePackageFsWatcher _csFileWatcher;

    public bool NeedsCompilation { get; private set; }

    private static int _newProjectPosition = 0;
}