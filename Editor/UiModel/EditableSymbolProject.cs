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

    private static readonly List<Action> PendingUpdateActions = new();

    public EditableSymbolProject(CsProjectFile csProjectFile)
        : base(csProjectFile.Assembly)
    {
        CsProjectFile = csProjectFile;
        csProjectFile.Recompiled += project => PendingUpdateActions.Add(() => UpdateSymbols(project));
        AllProjectsRw.Add(this);
        _fileSystemWatcher = new EditablePackageFsWatcher(this, OnFileChanged, OnFileRenamed);
    }

    private void UpdateSymbols(CsProjectFile project)
    {
        var operatorTypes = project.Assembly.OperatorTypes;
        Dictionary<Guid, Symbol> foundSymbols = new();
        foreach (var (guid, type) in operatorTypes)
        {
            if (Symbols.Remove(guid, out var symbol))
            {
                foundSymbols.Add(guid, symbol);
                symbol.UpdateInstanceType(type);
                symbol.CreateAnimationUpdateActionsForSymbolInstances();
                UpdateUiEntriesForSymbol(symbol);
            }
            else
            {
                // it's a new type!!
            }

            UpdateUiEntriesForSymbol(symbol);
        }

        // remaining symbols have been removed from the assembly
        while (Symbols.Count > 0)
        {
            var (guid, symbol) = Symbols.First();
            RemoveSymbol(guid);
        }

        Symbols.Clear();

        foreach (var (guid, symbol) in foundSymbols)
        {
            Symbols.Add(guid, symbol);
        }
    }

    private bool RemoveSymbol(Guid guid)
    {
        var removed = Symbols.Remove(guid, out var symbol);

        if (!removed)
            return false;

        SymbolRegistry.Entries.Remove(guid);
        SymbolUiRegistry.Entries.Remove(guid, out var symbolUi);

        return true;
    }

    public bool TryCreateHome()
    {
        if (!CsProjectFile.Assembly.HasHome)
            return false;

        var homeGuid = CsProjectFile.Assembly.HomeGuid;
        var symbol = Symbols[homeGuid];
        RootInstance = symbol.CreateInstance(HomeInstanceId);
        return true;
    }

    public void UpdateUiEntriesForSymbol(Symbol symbol, SymbolUi symbolUi = null)
    {
        if (SymbolUiRegistry.Entries.TryGetValue(symbol.Id, out var foundSymbolUi))
        {
            if (symbolUi != null)
            {
                SymbolUiRegistry.Entries[symbol.Id] = symbolUi;
                Log.Warning("Symbol UI for symbol " + symbol.Id + " already exists. Replacing.");
            }
            else
            {
                foundSymbolUi.UpdateConsistencyWithSymbol();
            }
        }
        else
        {
            symbolUi ??= new SymbolUi(symbol);
            SymbolUiRegistry.Entries.Add(symbol.Id, symbolUi);
            SymbolUis.Add(symbolUi);
        }
    }

    public void ReplaceSymbolUi(Symbol newSymbol, SymbolUi symbolUi = null)
    {
        AddSymbol(newSymbol);
        UpdateUiEntriesForSymbol(newSymbol, symbolUi);
        RegisterCustomChildUi(newSymbol);
    }


    private void AddSymbol(Symbol newSymbol)
    {
        SymbolRegistry.Entries.Add(newSymbol.Id, newSymbol);
        Symbols.Add(newSymbol.Id, newSymbol);
        AssemblyInformation.UpdateType(newSymbol.InstanceType, newSymbol.Id);
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

    public bool TryRecompileWithNewSource(Symbol symbol, string newSource)
    {
        // disable file watcher
        
        string currentSource;
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

        var success = CsProjectFile.TryRecompile(Compiler.BuildMode.Debug);

        if (!success && currentSource != string.Empty)
        {
            symbol.PendingSource = currentSource;
            WriteSymbolSourceToFile(symbol, filePathFmt);
        }

        UnmarkAsSaving();

        return success;
    }

    public bool TryCompile(string sourceCode, string newSymbolName, Guid newSymbolId, string ns, out Symbol newSymbol)
    {
        throw new NotImplementedException();
    }

    /// <returns>
    /// Returns true if the project does not need to be recompiled or if it successfully recompiled.
    /// </returns>
    public bool RecompileIfNecessary()
    {
        if (!_needsCompilation)
            return true;

        return CsProjectFile.TryRecompile(Compiler.BuildMode.Debug);
    }

    private static readonly Guid HomeInstanceId = Guid.Parse("12d48d5a-b8f4-4e08-8d79-4438328662f0");
    public override string Folder => CsProjectFile.Directory;

    public readonly CsProjectFile CsProjectFile;

    public static Instance RootInstance { get; private set; }

    public static EditableSymbolProject ActiveProject { get; private set; }
    private static readonly List<EditableSymbolProject> AllProjectsRw = new();
    public static readonly IReadOnlyList<EditableSymbolProject> AllProjects = AllProjectsRw;

    public override bool IsModifiable => true;

    private readonly EditablePackageFsWatcher _fileSystemWatcher;

    private bool _needsCompilation;

}