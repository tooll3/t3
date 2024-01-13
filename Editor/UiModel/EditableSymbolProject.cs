using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Compilation;
using T3.Editor.Gui.Windows;

namespace T3.Editor.UiModel;

internal sealed partial class EditableSymbolProject : EditorSymbolPackage
{
    protected override AssemblyInformation AssemblyInformation => CsProjectFile.Assembly;

    private static readonly Queue<Action> PendingUpdateActions = new();

    public EditableSymbolProject(CsProjectFile csProjectFile)
        : base(csProjectFile.Assembly)
    {
        CsProjectFile = csProjectFile;
        csProjectFile.Recompiled += project => PendingUpdateActions.Enqueue(() => UpdateSymbols(project));
        AllProjectsRw.Add(this);
        _fileSystemWatcher = new EditablePackageFsWatcher(this, OnFileChanged, OnFileRenamed);
    }

    public bool TryCreateHome()
    {
        if (!CsProjectFile.Assembly.HasHome)
            return false;

        Log.Info($"Creating home for {CsProjectFile.Name}...");
        var homeGuid = CsProjectFile.Assembly.HomeGuid;
        var symbol = Symbols[homeGuid];
        RootInstance = symbol.CreateInstance(HomeInstanceId);
        symbol.IsHome = true;
        return true;
    }

    public bool TryCompile(string sourceCode, string newSymbolName, Guid newSymbolId, string nameSpace, out Symbol newSymbol)
    {
        var pathFmt = BuildFilepathFmt(newSymbolName, nameSpace);
        var path = string.Format(pathFmt, SourceCodeExtension);

        try
        {
            File.WriteAllText(path, sourceCode);
            MarkAsModified();
        }
        catch
        {
            Log.Error($"Could not write source code to {path}");
            newSymbol = null;
            return false;
        }

        var recompiled = TryRecompile();
        
        if (!recompiled)
        {
            newSymbol = null;
            return false;
        }
        
        ExecutePendingUpdates();
        
        return Symbols.TryGetValue(newSymbolId, out newSymbol);
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
        string currentSource;
        newName ??= symbol.Name;
        try
        {
            currentSource = File.ReadAllText(symbol.SymbolFilePath);
        }
        catch
        {
            Log.Error($"Could not read original source code at \"{symbol.SymbolFilePath}\"");
            currentSource = string.Empty;
        }

        symbol.PendingSource = newSource;
        MarkAsSaving();

        var filePathFmt = BuildFilepathFmt(symbol);
        WriteSymbolSourceToFile(symbol, filePathFmt);

        var success = TryRecompile();

        if (success)
        {
            symbol.Name = newName;
        }
        else if (currentSource != string.Empty)
        {
            symbol.PendingSource = currentSource;
            WriteSymbolSourceToFile(symbol, filePathFmt);
        }

        UnmarkAsSaving();

        return success;
    }

    private bool TryRecompile()
    {
        SaveAll();
        return CsProjectFile.TryRecompile(Compiler.BuildMode.Debug, DateTime.UtcNow.Ticks);
    }

    private void UpdateSymbols(CsProjectFile project)
    {
        LocateSourceCodeFiles();
        EditorInitialization.UpdateSymbolPackage(this);
    }

    private void UpdateUiEntriesForSymbol(Symbol symbol)
    {
        if (SymbolUiRegistry.Entries.TryGetValue(symbol.Id, out var symbolUi))
        {
            symbolUi.UpdateConsistencyWithSymbol();
        }
        else
        {
            symbolUi = new SymbolUi(symbol);
            SymbolUiRegistry.EntriesEditable.Add(symbol.Id, symbolUi);
            SymbolUis.TryAdd(symbol.Id, symbolUi);
        }
    }

    public void ReplaceSymbolUi(SymbolUi symbolUi)
    {
        var symbol = symbolUi.Symbol;
        SymbolUiRegistry.EntriesEditable[symbol.Id] = symbolUi;
        SymbolUis[symbol.Id] = symbolUi; 
        //UpdateUiEntriesForSymbol(symbol);
        //RegisterCustomChildUi(symbol);
        
        Log.Debug($"Replaced symbol ui for {symbol.Name}");
    }

    public void RenameNameSpace(NamespaceTreeNode node, string nameSpace, EditableSymbolProject newDestinationProject)
    {
        var movingToAnotherPackage = newDestinationProject != this;

        var ogNameSpace = node.GetAsString();
        foreach (var symbol in Symbols.Values)
        {
            if (!symbol.Namespace.StartsWith(ogNameSpace))
                continue;

            //var newNameSpace = parent + "."
            var newNameSpace = Regex.Replace(symbol.Namespace, ogNameSpace, nameSpace);
            Log.Debug($" Changing namespace of {symbol.Name}: {symbol.Namespace} -> {newNameSpace}");
            symbol.Namespace = newNameSpace;

            if (!movingToAnotherPackage)
                continue;

            GiveSymbolToPackage(symbol, newDestinationProject);
        }
    }

    private void GiveSymbolToPackage(Symbol symbol, EditableSymbolProject newDestinationProject)
    {
        throw new NotImplementedException();
    }

    public void MarkAsModified()
    {
        _needsCompilation = true;
    }

    private static readonly Guid HomeInstanceId = Guid.Parse("12d48d5a-b8f4-4e08-8d79-4438328662f0");
    public override string Folder => CsProjectFile.Directory;

    public readonly CsProjectFile CsProjectFile;

    public static Instance RootInstance { get; private set; }

    internal static EditableSymbolProject ActiveProjectRw;
    public static EditableSymbolProject ActiveProject => ActiveProjectRw ??= AllProjectsRw.FirstOrDefault(x => x.AssemblyInformation.HasHome); // todo - userSettings recents
    private static readonly List<EditableSymbolProject> AllProjectsRw = new();
    public static readonly IReadOnlyList<EditableSymbolProject> AllProjects = AllProjectsRw;

    public override bool IsModifiable => true;

    private readonly EditablePackageFsWatcher _fileSystemWatcher;

    private bool _needsCompilation;

    public void ExecutePendingUpdates()
    {
        while (PendingUpdateActions.TryDequeue(out var action))
        {
            action.Invoke();
        }
    }
}