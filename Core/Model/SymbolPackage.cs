#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Core.Stats;

// ReSharper disable RedundantNameQualifier

namespace T3.Core.Model;

/// <summary>
/// Base class of essentially what is a read only project.
/// </summary>
/// <remarks>
/// Regarding naming, we consider all t3 operator packages as packages for the sake of consistency with future nuget terminology etc.
/// -- only the user's editable "packages" are referred to as projects
///</remarks>
public abstract partial class SymbolPackage : IResourcePackage
{
    public virtual AssemblyInformation AssemblyInformation { get; }
    public virtual string Folder => AssemblyInformation.Directory;
    
    public virtual string DisplayName => AssemblyInformation.Name;

    protected virtual IEnumerable<string> SymbolSearchFiles =>
        Directory.EnumerateFiles(Path.Combine(Folder, "Symbols"), $"*{SymbolExtension}", SearchOption.AllDirectories);

    protected event Action<string?, Symbol>? SymbolAdded;
    protected event Action<Symbol>? SymbolUpdated;
    protected event Action<Guid>? SymbolRemoved;

    public string ResourcesFolder { get; private set; } = null!;

    static SymbolPackage()
    {
        RenderStatsCollector.RegisterProvider(new OpUpdateCounter());
        RegisterTypes();
    }

    protected SymbolPackage(AssemblyInformation assembly)
    {
        AssemblyInformation = assembly;
    }

    public virtual void InitializeResources()
    {
        ResourcesFolder = Path.Combine(Folder, ResourceManager.ResourcesSubfolder);
        Directory.CreateDirectory(ResourcesFolder);
        ResourceManager.AddSharedResourceFolder(this, AssemblyInformation.ShouldShareResources);
    }

    public virtual void Dispose()
    {
        ResourceManager.RemoveSharedResourceFolder(this);
        ClearSymbols();

        AssemblyInformation.Unload();
        // Todo - symbol instance destruction...?
    }

    protected void ClearSymbols()
    {
        if (Symbols.Count == 0)
            return;

        var symbols = Symbols.Values.ToArray();
        foreach (var symbol in symbols)
        {
            var id = symbol.Id;
            Symbols.Remove(id, out _);
            SymbolRegistry.EntriesEditable.Remove(id, out _);
        }
    }

    public void LoadSymbols(bool parallel, out List<SymbolJson.SymbolReadResult> newlyRead, out List<Symbol> allNewSymbols, bool readOnlyReload)
    {
        Log.Debug($"{AssemblyInformation.Name}: Loading symbols...");

        ConcurrentDictionary<Guid, Type> newTypes = new();

        var removedSymbolIds = new HashSet<Guid>(Symbols.Keys);
        List<Symbol> updatedSymbols = new();

        if (readOnlyReload)
            ClearSymbols();

        Action<Guid, Type, ConcurrentDictionary<Guid, Type>> reloadFunc = readOnlyReload
                                            ? (guid, type, newTypesDict) => newTypesDict.TryAdd(guid, type)
                                            : (guid, type, newTypesDict) =>
                                              {
                                                  if (Symbols.TryGetValue(guid, out var symbol))
                                                  {
                                                      removedSymbolIds.Remove(guid);

                                                      symbol.UpdateType(type, this, out _);
                                                      updatedSymbols.Add(symbol);
                                                  }
                                                  else
                                                  {
                                                      // it's a new type!!
                                                      newTypesDict.TryAdd(guid, type);
                                                  }
                                              };

        if (parallel)
        {
            AssemblyInformation.OperatorTypeInfo
                               .AsParallel()
                               .ForAll(kvp => reloadFunc(kvp.Key, kvp.Value.Type, newTypes));
        }
        else
        {
            foreach (var (guid, type) in AssemblyInformation.OperatorTypeInfo)
            {
                reloadFunc(guid, type.Type, newTypes);
            }
        }

        foreach (var symbol in updatedSymbols)
        {
            UpdateSymbolInstances(symbol);
            SymbolUpdated?.Invoke(symbol);
        }

        if (SymbolRemoved != null)
        {
            // remaining symbols have been removed from the assembly
            foreach (var symbolId in removedSymbolIds)
            {
                SymbolRemoved.Invoke(symbolId);
            }
        }

        Func<Guid, Symbol, ConcurrentDictionary<Guid, Symbol>, bool> addSymbolFunc = readOnlyReload
                                                                               ? (id, symbol, localRegistry) => localRegistry.TryAdd(id, symbol)
                                                                                     && SymbolRegistry.EntriesEditable.TryAdd(id, symbol)
                                                                               : (id, symbol, localRegistry) =>
                                                                                 {
                                                                                     localRegistry[id] = symbol;
                                                                                     SymbolRegistry.EntriesEditable[id] = symbol;
                                                                                     return true;
                                                                                 };

        newlyRead = [];
        allNewSymbols = [];
        
        if (newTypes.Count > 0)
        {
            var searchFileEnumerator = parallel ? SymbolSearchFiles.AsParallel() : SymbolSearchFiles;
            var symbolsRead = searchFileEnumerator
                             .Select(JsonFileResult<Symbol>.ReadAndCreate)
                             .Select(result =>
                                     {
                                         if (!newTypes.TryGetValue(result.Guid, out var type))
                                             return default;

                                         return ReadSymbolFromJsonFileResult(result, type);
                                     })
                             .Where(symbolReadResult => symbolReadResult.Result.Symbol is not null)
                             .ToArray();

            Log.Debug($"{AssemblyInformation.Name}: Registering loaded symbols...");

            foreach (var readSymbolResult in symbolsRead)
            {
                var symbol = readSymbolResult.Result.Symbol;
                var id = symbol.Id;

                if (!addSymbolFunc(id, symbol, Symbols))
                {
                    Log.Error($"Can't load symbol for [{symbol.Name}]. Registry already contains id {symbol.Id}: [{SymbolRegistry.EntriesEditable[symbol.Id].Name}]");
                    continue;
                }

                newlyRead.Add(readSymbolResult.Result);
                newTypes.Remove(id, out _);
                allNewSymbols.Add(symbol);

                SymbolAdded?.Invoke(readSymbolResult.Path, symbol);
            }
        }

        // these do not have a file
        foreach (var (guid, newType) in newTypes)
        {
            var symbol = CreateSymbol(newType, guid);

            var id = symbol.Id;
            if (!addSymbolFunc(id, symbol, Symbols))
            {
                Log.Error($"{AssemblyInformation.Name}: Ignoring redefinition symbol {symbol.Name}.");
                continue;
            }

            //Log.Debug($"{AssemblyInformation.Name}: new added symbol: {newType}");

            allNewSymbols.Add(symbol);

            SymbolAdded?.Invoke(null, symbol);
        }

        return;

        SymbolJsonResult ReadSymbolFromJsonFileResult(JsonFileResult<Symbol> jsonInfo, Type type)
        {
            var result = SymbolJson.ReadSymbolRoot(jsonInfo.Guid, jsonInfo.JToken, type, this);

            jsonInfo.Object = result.Symbol;
            return new SymbolJsonResult(result, jsonInfo.FilePath);
        }
    }

    protected static void UpdateSymbolInstances(Symbol symbol)
    {
        symbol.UpdateInstanceType();
        symbol.CreateAnimationUpdateActionsForSymbolInstances();
    }

    internal Symbol CreateSymbol(Type instanceType, Guid id, Guid[]? orderedInputIds = null)
    {
        var symbol = new Symbol(instanceType, id, this, orderedInputIds);

        return symbol;
    }


    public void ApplySymbolChildren(List<SymbolJson.SymbolReadResult> symbolsRead)
    {
        Log.Debug($"{AssemblyInformation.Name}: Applying symbol children...");
        Parallel.ForEach(symbolsRead, result => TryReadAndApplyChildren(result));
        Log.Debug($"{AssemblyInformation.Name}: Done applying symbol children.");
    }

    protected static bool TryReadAndApplyChildren(SymbolJson.SymbolReadResult result)
    {
        if (!SymbolJson.TryReadAndApplySymbolChildren(result))
        {
            Log.Error($"Problem obtaining children of {result.Symbol.Name} ({result.Symbol.Id})");
            return false;
        }

        return true;
    }

    public readonly record struct SymbolJsonResult(in SymbolJson.SymbolReadResult Result, string Path);

    protected readonly ConcurrentDictionary<Guid, Symbol> Symbols = new();

    public const string SymbolExtension = ".t3";

    public bool ContainsSymbolName(string newSymbolName, string symbolNamespace)
    {
        foreach (var existing in Symbols.Values)
        {
            if (existing.Name == newSymbolName && existing.Namespace == symbolNamespace)
                return true;
        }

        return false;
    }

    public virtual ResourceFileWatcher? FileWatcher => null;
    public virtual bool IsReadOnly => true;
}