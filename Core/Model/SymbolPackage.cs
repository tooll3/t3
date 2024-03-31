#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    private static ConcurrentBag<SymbolPackage> _allPackages = [];
    public static IEnumerable<SymbolPackage> AllPackages => _allPackages;

    public string ResourcesFolder { get; private set; } = null!;

    static SymbolPackage()
    {
        RenderStatsCollector.RegisterProvider(new OpUpdateCounter());
        RegisterTypes();
    }

    protected SymbolPackage(AssemblyInformation assembly)
    {
        AssemblyInformation = assembly;
        lock(_allPackages)
            _allPackages.Add(this);
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
        
        
        var currentPackages = _allPackages.ToList();
        currentPackages.Remove(this);
        lock (_allPackages)
            _allPackages = new ConcurrentBag<SymbolPackage>(currentPackages);
        
        AssemblyInformation.Unload();
        // Todo - symbol instance destruction...?
    }

    private void ClearSymbols()
    {
        if (SymbolDict.Count == 0)
            return;

        var symbols = SymbolDict.Values.ToArray();
        foreach (var symbol in symbols)
        {
            var id = symbol.Id;
            SymbolDict.Remove(id, out _);
        }
    }

    public void LoadSymbols(bool parallel, out List<SymbolJson.SymbolReadResult> newlyRead, out List<Symbol> allNewSymbols)
    {
        Log.Debug($"{AssemblyInformation.Name}: Loading symbols...");

        ConcurrentDictionary<Guid, Type> newTypes = new();

        var removedSymbolIds = new HashSet<Guid>(SymbolDict.Keys);
        List<Symbol> updatedSymbols = new();

        if (parallel)
        {
            AssemblyInformation.OperatorTypeInfo
                               .AsParallel()
                               .ForAll(kvp => LoadTypes(kvp.Key, kvp.Value.Type, newTypes));
        }
        else
        {
            foreach (var (guid, type) in AssemblyInformation.OperatorTypeInfo)
            {
                LoadTypes(guid, type.Type, newTypes);
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

                if (!SymbolDict.TryAdd(id, symbol))
                {
                    Log.Error($"Can't load symbol for [{symbol.Name}]. Registry already contains id {symbol.Id}: [{SymbolDict[symbol.Id].Name}]");
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
            if (!SymbolDict.TryAdd(id, symbol))
            {
                Log.Error($"{AssemblyInformation.Name}: Ignoring redefinition symbol {symbol.Name}.");
                continue;
            }

            //Log.Debug($"{AssemblyInformation.Name}: new added symbol: {newType}");

            allNewSymbols.Add(symbol);

            SymbolAdded?.Invoke(null, symbol);
        }

        return;

        void LoadTypes(Guid guid, Type type, ConcurrentDictionary<Guid, Type> newTypesDict)
        {
            if (SymbolDict.TryGetValue(guid, out var symbol))
            {
                removedSymbolIds.Remove(guid);

                symbol.UpdateTypeWithoutUpdatingDefinitionsOrInstances(type, this);
                updatedSymbols.Add(symbol);
            }
            else
            {
                // it's a new type!!
                newTypesDict.TryAdd(guid, type);
            }
        }

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

    internal Symbol CreateSymbol(Type instanceType, Guid id)
    {
        return new Symbol(instanceType, id, this);
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

    public IReadOnlyDictionary<Guid, Symbol> Symbols => SymbolDict;
    protected readonly ConcurrentDictionary<Guid, Symbol> SymbolDict = new();

    public const string SymbolExtension = ".t3";

    public bool ContainsSymbolName(string newSymbolName, string symbolNamespace)
    {
        foreach (var existing in SymbolDict.Values)
        {
            if (existing.Name == newSymbolName && existing.Namespace == symbolNamespace)
                return true;
        }

        return false;
    }

    public virtual ResourceFileWatcher? FileWatcher => null;
    public virtual bool IsReadOnly => true;

    public bool TryGetSymbol(Guid symbolId, [NotNullWhen(true)] out Symbol? symbol) => SymbolDict.TryGetValue(symbolId, out symbol);
}