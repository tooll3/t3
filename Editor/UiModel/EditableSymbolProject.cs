using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Compilation;
using T3.Editor.Gui.Windows;

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
        _newProjectPosition += 100;

        return true;
    }

    public bool TryCompile(string sourceCode, string newSymbolName, Guid newSymbolId, string nameSpace, out Symbol newSymbol)
    {
        var pathFmt = BuildFilepathFmt(newSymbolName, nameSpace);
        var path = string.Format(pathFmt, SourceCodeExtension);

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
            return Symbols.TryGetValue(newSymbolId, out newSymbol);
        }

        newSymbol = null;
        return false;
    }

    /// <returns>
    /// Returns true if the project does not need to be recompiled or if it successfully recompiled.
    /// </returns>
    public bool RecompileIfNecessary()
    {
        if (!_needsCompilation)
            return true;

        return TryRecompile();
    }

    // todo : determine name from source code
    public bool TryRecompileWithNewSource(Symbol symbol, string newSource, string newName = null)
    {
        newName ??= symbol.Name;
        var gotCurrentSource = _sourceCodeFiles.TryGetValue(symbol.Id, out var currentSourcePath);
        if (!gotCurrentSource)
        {
            Log.Error($"Could not find original source code for symbol \"{symbol.Name}\"");
            return false;
        }

        string currentSourceCode;

        try
        {
            currentSourceCode = File.ReadAllText(currentSourcePath);
        }
        catch
        {
            Log.Error($"Could not read original source code at \"{currentSourcePath}\"");
            currentSourceCode = string.Empty;
        }

        symbol.PendingSource = newSource;
        symbol.Name = newName;

        var symbolUi = SymbolUis[symbol.Id];
        symbolUi.FlagAsModified();

        if (TryRecompile())
        {
            return true;
        }

        if (currentSourceCode != string.Empty)
        {
            symbol.PendingSource = currentSourceCode;
            symbolUi.FlagAsModified();
            SaveModifiedSymbols();
        }

        return false;
    }

    private bool TryRecompile()
    {
        MarkAsSaving();

        _needsCompilation = false;
        SaveModifiedSymbols();
        var updated = CsProjectFile.TryRecompile(Compiler.BuildMode.Debug);

        UnmarkAsSaving();

        if (!updated)
            return false;

        UpdateSymbols();
        return true;
    }

    private void UpdateSymbols()
    {
        LocateSourceCodeFiles();
        ProjectSetup.UpdateSymbolPackage(this);
    }

    public void ReplaceSymbolUi(SymbolUi symbolUi)
    {
        var symbol = symbolUi.Symbol;
        SymbolUiRegistry.EntriesEditable[symbol.Id] = symbolUi;
        SymbolUis[symbol.Id] = symbolUi;

        Log.Debug($"Replaced symbol ui for {symbol.Name}");
    }

    public void RenameNameSpace(string sourceNamespace, string newNamespace, EditableSymbolProject newDestinationProject)
    {
        var movingToAnotherPackage = newDestinationProject != this;

        var moved = false;
        
        // copy since we are modifying the collection while iterating
        var mySymbols = Symbols.Values.ToArray();
        foreach (var symbol in mySymbols)
        {
            if (!symbol.Namespace.StartsWith(sourceNamespace))
                continue;

            var newNameSpace = Regex.Replace(symbol.Namespace, sourceNamespace, newNamespace);
            moved = true;
            
            var id = symbol.Id;

            if (_sourceCodeFiles.TryGetValue(id, out var sourceCodePath))
            {
                var sourceCode = File.ReadAllText(sourceCodePath);
                var newSourceCode = Regex.Replace(sourceCode, sourceNamespace, newNamespace);
                symbol.PendingSource = newSourceCode;
            }

            Log.Debug($" Changing namespace of {symbol.Name}: {symbol.Namespace} -> {newNameSpace}");
            symbol.Namespace = newNameSpace;

            var symbolUi = SymbolUis[id];
            symbolUi.FlagAsModified();

            if (movingToAnotherPackage)
            {
                GiveSymbolToPackage(id, newDestinationProject);
            }
        }

        if (moved)
        {
            MarkAsNeedingRecompilation();
        }
    }

    private void GiveSymbolToPackage(Guid id, EditableSymbolProject newDestinationProject)
    {
        Symbols.Remove(id, out var symbol);
        SymbolUis.Remove(id, out var symbolUi);
        _sourceCodeFiles.Remove(id, out var sourceCodePath);
        
        symbol!.SymbolPackage = newDestinationProject;

        newDestinationProject.Symbols.Add(id, symbol);
        newDestinationProject.SymbolUis.TryAdd(id, symbolUi);
        newDestinationProject._sourceCodeFiles.TryAdd(id, sourceCodePath);

        symbolUi!.FlagAsModified();
        newDestinationProject.MarkAsNeedingRecompilation();
        MarkAsNeedingRecompilation();
    }

    private void MarkAsNeedingRecompilation()
    {
        if (IsSaving)
            return;
        _needsCompilation = true;
    }

    public override string Folder => CsProjectFile.Directory;

    public readonly CsProjectFile CsProjectFile;

    private static readonly List<EditableSymbolProject> AllProjectsRw = new();
    public static readonly IReadOnlyList<EditableSymbolProject> AllProjects = AllProjectsRw;

    public override bool IsModifiable => true;

    private readonly EditablePackageFsWatcher _csFileWatcher;

    private bool _needsCompilation;

    private static int _newProjectPosition = 0;
}