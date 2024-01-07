using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Editor.Compilation;
using T3.Editor.Gui.Windows;

namespace T3.Editor.UiModel;

internal sealed class EditableSymbolProject : EditorSymbolPackage
{
    protected override AssemblyInformation AssemblyInformation => CsProjectFile.Assembly;

    private static readonly List<Action> PendingUpdateActions = new();

    public EditableSymbolProject(CsProjectFile csProjectFile)
        : base(csProjectFile.Assembly)
    {
        CsProjectFile = csProjectFile;
        csProjectFile.Recompiled += project => PendingUpdateActions.Add(() => UpdateSymbols(project));
        SymbolDataByProjectRw.Add(csProjectFile, this);
        _fileSystemWatcher = new EditablePackageFSWatcher(this);
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

    public void SaveAll()
    {
        Log.Debug($"{AssemblyInformation.Name}: Saving...");

        MarkAsSaving();

        // Save all t3 and source files
        RemoveAllSymbolFiles();
        SortAllSymbolSourceFiles();
        SaveSymbolDefinitionAndSourceFiles(Symbols.Values);

        // Remove all old ui files before storing to get rid off invalid ones
        // TODO: this also seems dangerous, similar to how the Symbol SaveAll works
        var symbolUiFiles = Directory.GetFiles(Folder, $"*{SymbolUiExtension}", SearchOption.AllDirectories);
        foreach (var filepath in symbolUiFiles)
        {
            try
            {
                File.Delete(filepath);
            }
            catch (Exception e)
            {
                Log.Warning("Failed to deleted file '" + filepath + "': " + e);
            }
        }

        WriteSymbolUis(SymbolUis);

        UnmarkAsSaving();
    }

    /// <summary>
    /// Note: This does NOT clean up 
    /// </summary>
    internal void SaveModifiedSymbols()
    {
        MarkAsSaving();
        try
        {
            var modifiedSymbolUis = SymbolUis.Where(symbolUi => symbolUi.HasBeenModified).ToList();
            Log.Debug($"Saving {modifiedSymbolUis.Count} modified symbols...");

            var modifiedSymbols = modifiedSymbolUis.Select(symbolUi => symbolUi.Symbol).ToList();
            SaveSymbolDefinitionAndSourceFiles(modifiedSymbols);
            WriteSymbolUis(modifiedSymbolUis);
        }
        catch (InvalidOperationException e)
        {
            Log.Warning($"Saving failed. Please try to save manually ({e.Message})");
        }

        UnmarkAsSaving();
    }

    private void WriteSymbolUis(IEnumerable<SymbolUi> symbolUis)
    {
        foreach (var symbolUi in symbolUis)
        {
            var symbol = symbolUi.Symbol;
            var symbolFilePath = BuildFilepathForSymbol(symbol, SymbolUiExtension);

            using (var sw = new StreamWriter(symbolFilePath))
            using (var writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;
                SymbolUiJson.WriteSymbolUi(symbolUi, writer);
            }

            symbolUi.ClearModifiedFlag();
        }
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

    private void SaveSymbolDefinitionAndSourceFiles(IEnumerable<Symbol> symbols)
    {
        foreach (var symbol in symbols)
        {
            var filepath = BuildFilepathForSymbol(symbol, SymbolExtension);

            using var sw = new StreamWriter(filepath);
            using var writer = new JsonTextWriter(sw);
            writer.Formatting = Formatting.Indented;
            SymbolJson.WriteSymbol(symbol, writer);

            if (!string.IsNullOrWhiteSpace(symbol.PendingSource))
            {
                WriteSymbolSourceToFile(symbol);
            }
        }
    }

    private void SortAllSymbolSourceFiles()
    {
        // Move existing source files to correct namespace folder
        var sourceFiles = Directory.GetFiles(Folder, $"*{SourceCodeExtension}", SearchOption.AllDirectories);
        foreach (var sourceFilePath in sourceFiles)
        {
            var classname = Path.GetFileNameWithoutExtension(sourceFilePath);
            var symbol = SymbolRegistry.Entries.Values.SingleOrDefault(s => s.Name == classname);
            if (symbol == null)
            {
                // This happens when renaming symbols.
                Log.Debug($"Skipping unregistered source file {sourceFilePath}");
                continue;
            }

            var targetFilepath = BuildFilepathForSymbol(symbol, SourceCodeExtension);
            if (sourceFilePath == targetFilepath)
                continue;

            Log.Debug($" Moving {sourceFilePath} -> {targetFilepath} ...");
            try
            {
                File.Move(sourceFilePath, targetFilepath);
            }
            catch (Exception e)
            {
                Log.Warning("Failed to write source file '" + sourceFilePath + "': " + e);
            }
        }
    }

    private void WriteSymbolSourceToFile(Symbol symbol, string sourcePath = null)
    {
        sourcePath ??= BuildFilepathForSymbol(symbol, SourceCodeExtension);
        using (var sw = new StreamWriter(sourcePath))
        {
            sw.Write(symbol.PendingSource);
        }

        // Remove old source file and its entry in project
        if (!string.IsNullOrEmpty(symbol.DeprecatedSourcePath))
        {
            if (symbol.DeprecatedSourcePath == sourcePath)
            {
                Log.Warning($"Attempted to deprecated valid source file: {symbol.DeprecatedSourcePath}");
                symbol.DeprecatedSourcePath = string.Empty;
                return;
            }

            File.Delete(symbol.DeprecatedSourcePath);
            symbol.DeprecatedSourcePath = string.Empty;
        }

        symbol.PendingSource = null;
    }

    #region File path handling
    private string BuildFilepathForSymbols(Symbol symbol, string extension)
    {
        var dir = BuildAndCreateFolderFromNamespace(Folder, symbol.Namespace);
        return extension == SourceCodeExtension
                   ? Path.Combine(dir, symbol.Name + extension)
                   : Path.Combine(dir, symbol.Name + '_' + symbol.Id + extension);

        string BuildAndCreateFolderFromNamespace(string rootFolder, string symbolNamespace)
        {
            if (string.IsNullOrEmpty(symbolNamespace) || symbolNamespace == AssemblyInformation.Name)
            {
                return rootFolder;
            }

            var namespaceParts = symbolNamespace.Split('.');
            var assemblyRootParts = Folder.Split(Path.DirectorySeparatorChar);
            var rootOfOperatorDirectoryIndex = Array.IndexOf(assemblyRootParts, AssemblyInformation.Name);
            var operatorRootParts = assemblyRootParts.AsSpan()[..(rootOfOperatorDirectoryIndex)].ToArray();

            var directory = Path.Combine(operatorRootParts.Concat(namespaceParts).ToArray());
            Directory.CreateDirectory(directory);
            return directory;
        }
    }
    #endregion

    public void ReplaceSymbolUi(Symbol newSymbol, SymbolUi symbolUi = null)
    {
        base.AddSymbol(newSymbol);
        UpdateUiEntriesForSymbol(newSymbol, symbolUi);
        RegisterCustomChildUi(newSymbol);
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

    public void MarkAsModified()
    {
        _needsCompilation = true;
    }

    public bool TryRecompileWithNewSource(Symbol symbol, string newSource)
    {
        // disable file watcher

        var path = BuildFilepathForSymbol(symbol, SourceCodeExtension);
        var currentSource = File.ReadAllText(path);
        symbol.PendingSource = newSource;
        MarkAsSaving();
        WriteSymbolSourceToFile(symbol, path);

        var success = CsProjectFile.TryRecompile(Compiler.BuildMode.Debug);

        if (!success)
        {
            symbol.PendingSource = currentSource;
            WriteSymbolSourceToFile(symbol, path);
        }

        UnmarkAsSaving();

        return success;
    }

    public bool TryCompile(string sourceCode, string newSymbolName, Guid newSymbolId, string ns, out Symbol newSymbol)
    {
        throw new NotImplementedException();
    }

    private void MarkAsSaving()
    {
        Interlocked.Increment(ref _savingCount);
        _fileSystemWatcher.EnableRaisingEvents = false;
    }

    private void UnmarkAsSaving()
    {
        Interlocked.Decrement(ref _savingCount);
        _fileSystemWatcher.EnableRaisingEvents = true;
    }

    private static readonly Guid HomeInstanceId = Guid.Parse("12d48d5a-b8f4-4e08-8d79-4438328662f0");
    public override string Folder => CsProjectFile.Directory;

    public readonly CsProjectFile CsProjectFile;

    public static Instance RootInstance { get; private set; }

    public static IReadOnlyDictionary<CsProjectFile, EditableSymbolProject> SymbolDataByProject => SymbolDataByProjectRw;
    private static readonly Dictionary<CsProjectFile, EditableSymbolProject> SymbolDataByProjectRw = new();

    public static EditableSymbolProject ActiveProject { get; private set; }

    public override bool IsModifiable => true;

    private readonly EditablePackageFSWatcher _fileSystemWatcher;

    private bool _needsCompilation;

    public static bool IsSaving => Interlocked.Read(ref _savingCount) > 0;
    private static long _savingCount;
}

class EditablePackageFSWatcher : FileSystemWatcher
{
    public EditablePackageFSWatcher(EditableSymbolProject project) : base(project.Folder, "*.cs")
    {
        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName;

        IncludeSubdirectories = true;
        _project = project;
        Changed += OnChangeEvent;
        Created += OnChangeEvent;
        Deleted += OnChangeEvent;
        Renamed += OnChangeEvent;
    }

    private void OnChangeEvent(object sender, FileSystemEventArgs args)
    {
        _project.MarkAsModified();
    }

    private readonly EditableSymbolProject _project;
}