using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Core.Stats;

// ReSharper disable RedundantNameQualifier

namespace T3.Core.Model;

public partial class SymbolData
{
    public AssemblyInformation AssemblyInformation { get; }

    public static EventHandler<Assembly> AssemblyAdded;

    static SymbolData()
    {
        _updateCounter = new OpUpdateCounter();
        RegisterTypes();
    }

    public SymbolData(AssemblyInformation assembly)
    {
        AssemblyInformation = assembly;
        Folder = OperatorDirectoryName + Path.DirectorySeparatorChar + assembly.Name;
        SymbolDatas.Add(assembly, this);
    }

    public virtual void Load(bool enableLog)
    {
        Log.Debug("Loading symbols...");
        var symbolFiles = Directory.GetFiles(Folder, $"*{SymbolExtension}", SearchOption.AllDirectories);

        var symbolsRead = symbolFiles.AsParallel()
                                     .Select(JsonFileResult<Symbol>.ReadAndCreate)
                                     .Select(ReadSymbolFromJsonFileResult)
                                     .Where(symbolReadResult => symbolReadResult.Symbol is not null)
                                     .ToList(); // Execute and bring back to main thread

        Log.Debug("Registering loaded symbols...");

        // Check if there are symbols without a file, if yes add these
        var instanceTypesWithoutFile = AssemblyInformation.Assembly.ExportedTypes
                                                          .AsParallel()
                                                          .Where(type => type.IsSubclassOf(typeof(Instance)))
                                                          .Where(type => !type.IsGenericType)
                                                          .ToHashSet();

        foreach (var readSymbolResult in symbolsRead)
        {
            var symbol = readSymbolResult.Symbol;

            if (!TryAddSymbolTo(_symbols, symbol))
                continue;

            if (!TryAddSymbolTo(SymbolRegistry.Entries, symbol))
                continue;

            if (!SymbolOwnersEditable.TryAdd(symbol.Id, this))
            {
                Log.Error($"Duplicate symbol id {symbol.Id}");
            }

            instanceTypesWithoutFile.Remove(symbol.InstanceType);
            symbol.SymbolData = this;
        }

        foreach (var newType in instanceTypesWithoutFile)
        {
            RegisterTypeWithoutFile(newType);
        }

        Log.Debug("Applying symbol children...");
        Parallel.ForEach(symbolsRead, ReadAndApplyChildren);

        return;

        void ReadAndApplyChildren(SymbolJson.SymbolReadResult readSymbolResult)
        {
            var gotSymbolChildren = SymbolJson.TryReadAndApplySymbolChildren(readSymbolResult);
            if (!gotSymbolChildren)
            {
                Log.Error($"Problem obtaining children of {readSymbolResult.Symbol.Name} ({readSymbolResult.Symbol.Id})");
            }
        }

        SymbolJson.SymbolReadResult ReadSymbolFromJsonFileResult(JsonFileResult<Symbol> jsonInfo)
        {
            var result = SymbolJson.ReadSymbolRoot(jsonInfo.Guid, jsonInfo.JToken, allowNonOperatorInstanceType: false, AssemblyInformation);
            
            jsonInfo.Object = result.Symbol;
            return result;
        }

        void RegisterTypeWithoutFile(Type newType)
        {
            var typeNamespace = newType.Namespace;
            if (string.IsNullOrWhiteSpace(typeNamespace))
            {
                Log.Error($"Null or empty namespace of type {newType.Name}");
                return;
            }

            var @namespace = _innerNamespace.Replace(newType.Namespace ?? string.Empty, "").ToLower();
            var idFromNamespace = _idFromNamespace
                                 .Match(newType.Namespace ?? string.Empty).Value
                                 .Replace('_', '-');

            Debug.Assert(!string.IsNullOrWhiteSpace(idFromNamespace));
            var symbol = new Symbol(newType, Guid.Parse(idFromNamespace))
                             {
                                 Namespace = @namespace,
                                 Name = newType.Name
                             };

            var added = SymbolRegistry.Entries.TryAdd(symbol.Id, symbol);
            if (!added)
            {
                Log.Error($"Ignoring redefinition symbol {symbol.Name}. Please fix multiple definitions in Operators/Types/ folder");
                return;
            }

            if (enableLog)
                Log.Debug($"new added symbol: {newType}");
        }

        static bool TryAddSymbolTo(Dictionary<Guid, Symbol> collection, Symbol symbol)
        {
            var added = collection.TryAdd(symbol.Id, symbol);
            if (!added)
            {
                var existingSymbol = collection[symbol.Id];
                Log.Error($"Symbol {existingSymbol.Name} {symbol.Id} exists multiple times in database.");
            }

            return added;
        }
    }

    private readonly Regex _innerNamespace = new(@".Id_(\{){0,1}[0-9a-fA-F]{8}_[0-9a-fA-F]{4}_[0-9a-fA-F]{4}_[0-9a-fA-F]{4}_[0-9a-fA-F]{12}(\}){0,1}",
                                                 RegexOptions.IgnoreCase);

    private readonly Regex _idFromNamespace = new(@"(\{){0,1}[0-9a-fA-F]{8}_[0-9a-fA-F]{4}_[0-9a-fA-F]{4}_[0-9a-fA-F]{4}_[0-9a-fA-F]{12}(\}){0,1}",
                                                  RegexOptions.IgnoreCase);

    public virtual void SaveAll()
    {
        ResourceFileWatcher.DisableOperatorFileWatcher(Folder); // don't update ops if file is written during save

        MarkAsSaving();
        RemoveAllSymbolFiles();
        SortAllSymbolSourceFiles();
        SaveSymbolDefinitionAndSourceFiles(_symbols.Values);
        UnmarkAsSaving();

        ResourceFileWatcher.EnableOperatorFileWatcher(Folder);
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
        var sourceFiles = Directory.GetFiles(Folder, $"*{SourceExtension}", SearchOption.AllDirectories);
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

            var targetFilepath = BuildFilepathForSymbol(symbol, SourceExtension);
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

    private void WriteSymbolSourceToFile(Symbol symbol)
    {
        var sourcePath = BuildFilepathForSymbol(symbol, SourceExtension);
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
    public string BuildFilepathForSymbol(Symbol symbol, string extension)
    {
        var dir = BuildAndCreateFolderFromNamespace(symbol.Namespace, Folder);
        return extension == SourceExtension
                   ? Path.Combine(dir, symbol.Name + extension)
                   : Path.Combine(dir, symbol.Name + "_" + symbol.Id + extension);

        static string BuildAndCreateFolderFromNamespace(string symbolNamespace, string rootFolder)
        {
            var subdirectory = symbolNamespace.Trim().Replace('.', Path.DirectorySeparatorChar);
            var directory = Path.Combine(rootFolder, subdirectory);
            Directory.CreateDirectory(directory);

            return directory;
        }
    }
    #endregion

    public virtual void AddSymbol(Symbol newSymbol)
    {
        SymbolRegistry.Entries.Add(newSymbol.Id, newSymbol);
        _symbols.Add(newSymbol.Id, newSymbol);
    }

    private static readonly OpUpdateCounter _updateCounter;

    public const string SourceExtension = ".cs";
    public const string SymbolExtension = ".t3";
    public const string SymbolUiExtension = ".t3ui";
    public const string OperatorDirectoryName = "Operators";
    protected string Folder { get; init; }

    public static bool IsSaving => Interlocked.Read(ref _savingCount) > 0;

    private static long _savingCount;

    private static readonly ConcurrentDictionary<Guid, SymbolData> SymbolOwnersEditable = new();
    public static IReadOnlyDictionary<Guid, SymbolData> SymbolOwners => SymbolOwnersEditable;

    private readonly Dictionary<Guid, Symbol> _symbols = new();
    private static readonly Dictionary<AssemblyInformation, SymbolData> SymbolDatas = new();
}