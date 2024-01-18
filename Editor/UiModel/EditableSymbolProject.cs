using System;
using System.Collections.Generic;
using System.IO;
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
        csProjectFile.Recompiled += project => PendingUpdateActions.Enqueue(() => UpdateSymbols(project));
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

        _rootSymbolUi.AddChild(homeSymbol, Guid.NewGuid(), new Vector2(0, _newProjectPosition), SymbolChildUi.DefaultOpSize, "");
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
            MarkAsModified();
        }
        catch
        {
            Log.Error($"Could not write source code to {path}");
            newSymbol = null;
            return false;
        }

        if (TryRecompile())
        {
            ExecutePendingUpdates();
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

        if (TryRecompile())
        {
            ExecutePendingUpdates();
            return true;
        }

        return false;
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
        MarkAsSaving();

        var filePathFmt = BuildFilepathFmt(symbol);
        WriteSymbolSourceToFile(symbol, filePathFmt);

        if (TryRecompile())
        {
            symbol.Name = newName;
            ExecutePendingUpdates();
            UnmarkAsSaving();
            return true;
        }

        if (currentSourceCode != string.Empty)
        {
            symbol.PendingSource = currentSourceCode;
            WriteSymbolSourceToFile(symbol, filePathFmt);
        }

        UnmarkAsSaving();
        return false;
    }

    private bool TryRecompile()
    {
        SaveAll();
        _needsCompilation = false;
        return CsProjectFile.TryRecompile(Compiler.BuildMode.Debug);
    }

    private void UpdateSymbols(CsProjectFile project)
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

    private void MarkAsModified()
    {
        _needsCompilation = true;
    }

    private void ExecutePendingUpdates()
    {
        while (PendingUpdateActions.TryDequeue(out var action))
        {
            action.Invoke();
        }
    }

    public override string Folder => CsProjectFile.Directory;

    public readonly CsProjectFile CsProjectFile;

    private static readonly List<EditableSymbolProject> AllProjectsRw = new();
    public static readonly IReadOnlyList<EditableSymbolProject> AllProjects = AllProjectsRw;

    public override bool IsModifiable => true;

    private readonly EditablePackageFsWatcher _csFileWatcher;

    private bool _needsCompilation;

    private static readonly Queue<Action> PendingUpdateActions = new();
    private static int _newProjectPosition = 0;
}