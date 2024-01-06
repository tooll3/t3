using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Editor.Compilation;
using T3.Editor.Gui.Windows;

namespace T3.Editor.UiModel;

internal sealed class EditableSymbolPackage : EditorSymbolPackage
{
    public EditableSymbolPackage(CsProjectFile csProjectFile)
        : base(csProjectFile.Assembly)
    {
        CsProjectFile = csProjectFile;
        SymbolDataByProjectRw.Add(csProjectFile, this);
        _fileSystemWatcher = new EditablePackageFSWatcher(this);
    }

    // todo - "home" should be marked by an attribute rather than a hard-coded id
    public bool TryCreateHome()
    {
        if (!CsProjectFile.Assembly.HasHome)
            return false;

        var homeGuid = CsProjectFile.Assembly.HomeGuid;
        var symbol = Symbols[homeGuid];
        RootInstance = symbol.CreateInstance(HomeInstanceId);
        return true;
    }

    public override void SaveAll()
    {
        Log.Debug($"{AssemblyInformation.Name}: Saving...");

        MarkAsSaving();

        // Save all t3 and source files
        base.SaveAll();

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
            foundSymbolUi.UpdateConsistencyWithSymbol();

            if (symbolUi != null)
            {
                Log.Warning("Symbol UI for symbol " + symbol.Id + " already exists. Disregarding new UI.");
            }
        }
        else
        {
            symbolUi ??= new SymbolUi(symbol);
            SymbolUiRegistry.Entries.Add(symbol.Id, symbolUi);
            SymbolUis.Add(symbolUi);
        }
    }

    public virtual void SaveAll()
    {
        MarkAsSaving();
        RemoveAllSymbolFiles();
        SortAllSymbolSourceFiles();
        SaveSymbolDefinitionAndSourceFiles(Symbols.Values);
        UnmarkAsSaving();
    }

    protected void MarkAsSaving() => Interlocked.Increment(ref _savingCount);
    protected void UnmarkAsSaving() => Interlocked.Decrement(ref _savingCount);

    protected void SaveSymbolDefinitionAndSourceFiles(IEnumerable<Symbol> symbols)
    {
        foreach (var symbol in symbols)
        {
            var filepath = BuildFilepathForSymbol(symbol, SymbolExtension);

            using (var sw = new StreamWriter(filepath))
            using (var writer = new JsonTextWriter(sw) { Formatting = Formatting.Indented })
            {
                SymbolJson.WriteSymbol(symbol, writer);
            }

            if (!string.IsNullOrEmpty(symbol.PendingSource))
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

    // Todo: this sounds like a dangerous step. we should overwrite these files by default and can check which files are not overwritten to delete others?
    private void RemoveAllSymbolFiles()
    {
        // Remove all old t3 files before storing to get rid off invalid ones
        var symbolFiles = Directory.GetFiles(Folder, $"*{SymbolExtension}", SearchOption.AllDirectories);
        foreach (var symbolFilePath in symbolFiles)
        {
            try
            {
                File.Delete(symbolFilePath);
            }
            catch (Exception e)
            {
                Log.Warning("Failed to deleted file '" + symbolFilePath + "': " + e);
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

            // Adjust path of file resource
            ResourceManager.RenameOperatorResource(symbol.DeprecatedSourcePath, sourcePath);

            symbol.DeprecatedSourcePath = string.Empty;
        }

        symbol.PendingSource = null;
    }

    #region File path handling
    private string BuildFilepathForSymbol(Symbol symbol, string extension)
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

    public void AddSymbol(Symbol newSymbol, SymbolUi symbolUi = null)
    {
        base.AddSymbol(newSymbol);
        UpdateUiEntriesForSymbol(newSymbol, symbolUi);
        RegisterCustomChildUi(newSymbol);
    }

    public void RenameNameSpace(NamespaceTreeNode node, string nameSpace, EditableSymbolPackage newDestinationPackage)
    {
        var movingToAnotherPackage = newDestinationPackage != this;

        var orgNameSpace = node.GetAsString();
        foreach (var symbol in Symbols.Values)
        {
            if (!symbol.Namespace.StartsWith(orgNameSpace))
                continue;

            //var newNameSpace = parent + "."
            var newNameSpace = Regex.Replace(symbol.Namespace, orgNameSpace, nameSpace);
            Log.Debug($" Changing namespace of {symbol.Name}: {symbol.Namespace} -> {newNameSpace}");
            symbol.Namespace = newNameSpace;

            if (!movingToAnotherPackage)
                continue;

            GiveSymbolToPackage(symbol, newDestinationPackage);
        }
    }

    public void MarkAsModified()
    {
        _needsCompilation = true;
    }

    private static readonly Guid HomeInstanceId = Guid.Parse("12d48d5a-b8f4-4e08-8d79-4438328662f0");
    public override string Folder => CsProjectFile.Directory;

    public readonly CsProjectFile CsProjectFile;

    public static Instance RootInstance { get; private set; }

    public static IReadOnlyDictionary<CsProjectFile, EditableSymbolPackage> SymbolDataByProject => SymbolDataByProjectRw;
    private static readonly Dictionary<CsProjectFile, EditableSymbolPackage> SymbolDataByProjectRw = new();

    public static EditableSymbolPackage ActiveProject { get; private set; }

    public override bool IsModifiable => true;

    private readonly EditablePackageFSWatcher _fileSystemWatcher;

    private bool _needsCompilation;

    public bool TryRecompileWithNewSource(Symbol symbol, string newSource)
    {
        // disable file watcher
        
        var path = BuildFilepathForSymbol(symbol, SourceCodeExtension);
        var currentSource = File.ReadAllText(path);
        symbol.PendingSource = newSource;
        _fileSystemWatcher.EnableRaisingEvents = false;
        WriteSymbolSourceToFile(symbol, path);

        var success = CsProjectFile.TryRecompile(Compiler.BuildMode.Debug);

        if (!success)
        {
            symbol.PendingSource = currentSource;
            WriteSymbolSourceToFile(symbol, path);
        }
        
        _fileSystemWatcher.EnableRaisingEvents = true;

        return success;
    }

    public bool TryCompile(string sourceCode, string newSymbolName, Guid newSymbolId, string ns, out Symbol newSymbol)
    {
        throw new NotImplementedException();
    }
}

class EditablePackageFSWatcher : FileSystemWatcher
{
    public EditablePackageFSWatcher(EditableSymbolPackage package) : base(package.Folder, "*.cs")
    {
        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName;

        IncludeSubdirectories = true;
        _package = package;
        Changed += OnChangeEvent;
        Created += OnChangeEvent;
        Deleted += OnChangeEvent;
        Renamed += OnChangeEvent;
    }

    private void OnChangeEvent(object sender, FileSystemEventArgs args)
    {
        _package.MarkAsModified();
    }

    private readonly EditableSymbolPackage _package;
}