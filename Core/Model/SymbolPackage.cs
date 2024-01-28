using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Core.Stats;

// ReSharper disable RedundantNameQualifier

namespace T3.Core.Model;

public abstract partial class SymbolPackage
{
    public abstract AssemblyInformation AssemblyInformation { get; }
    public abstract string Folder { get; }

    protected internal ResourceFileWatcher ResourceFileWatcher { get; private set; }

    internal string ResourcesFolder => ResourceFileWatcher.WatchedFolder;
    protected abstract string ResourcesSubfolder { get; }

    static SymbolPackage()
    {
        _updateCounter = new OpUpdateCounter();
        RegisterTypes();
    }

    protected void InitializeFileWatcher()
    {
        if (ResourceFileWatcher != null)
            return;

        var fullResourcesFolder = Path.Combine(Folder, ResourcesSubfolder);
        ResourceFileWatcher = new ResourceFileWatcher(fullResourcesFolder, AssemblyInformation.ShouldShareResources);
    }

    public void LoadSymbols(bool enableLog, out List<SymbolJson.SymbolReadResult> newlyRead, out IReadOnlyCollection<Symbol> allNewSymbols)
    {
        Log.Debug($"{AssemblyInformation.Name}: Loading symbols...");

        Dictionary<Guid, Type> newTypes = new();

        var removedSymbolIds = new HashSet<Guid>(Symbols.Keys);
        foreach (var (guid, type) in AssemblyInformation.OperatorTypes)
        {
            if (Symbols.Count > 0 && Symbols.TryGetValue(guid, out var symbol))
            {
                removedSymbolIds.Remove(guid);
                symbol.UpdateInstanceType(type);
                symbol.CreateAnimationUpdateActionsForSymbolInstances();
            }
            else
            {
                // it's a new type!!
                newTypes.Add(guid, type);
            }
        }

        // remaining symbols have been removed from the assembly
        foreach (var symbolId in removedSymbolIds)
        {
            RemoveSymbol(symbolId);
        }

        newlyRead = [];
        List<Symbol> allNewSymbolsList = [];

        if (newTypes.Count > 0)
        {
            var symbolFiles = Directory.EnumerateFiles(Folder, $"*{SymbolExtension}", SearchOption.AllDirectories);
            var symbolsRead = symbolFiles
                             .AsParallel()
                             .Select(JsonFileResult<Symbol>.ReadAndCreate)
                             .Where(result => newTypes.ContainsKey(result.Guid))
                             .Select(ReadSymbolFromJsonFileResult)
                             .Where(symbolReadResult => symbolReadResult.Result.Symbol is not null)
                             .ToArray(); // Execute and bring back to main thread

            Log.Debug($"{AssemblyInformation.Name}: Registering loaded symbols...");

            foreach (var readSymbolResult in symbolsRead)
            {
                var symbol = readSymbolResult.Result.Symbol;

                var added = Symbols.TryAdd(symbol.Id, symbol)
                            && SymbolRegistry.EntriesEditable.TryAdd(symbol.Id, symbol);

                if (!added)
                {
                    Log.Error($"Can't load symbol for [{symbol.Name}]. Registry already contains id {symbol.Id}.");
                    continue;
                }

                newlyRead.Add(readSymbolResult.Result);
                newTypes.Remove(symbol.Id);
                allNewSymbolsList.Add(symbol);
                symbol.SymbolFilePath = readSymbolResult.Path;
            }
        }

        // these do not have a file
        foreach (var (guid, newType) in newTypes)
        {
            var @namespace = newType.Namespace;

            if (string.IsNullOrWhiteSpace(@namespace))
            {
                // set namespace to assembly name
                @namespace = AssemblyInformation.Name;
            }

            var symbol = CreateSymbol(newType, guid, newType.Name, @namespace);

            var added = Symbols.TryAdd(symbol.Id, symbol)
                        && SymbolRegistry.EntriesEditable.TryAdd(symbol.Id, symbol);
            if (!added)
            {
                Log.Error($"{AssemblyInformation.Name}: Ignoring redefinition symbol {symbol.Name}.");
                continue;
            }

            if (enableLog)
                Log.Debug($"{AssemblyInformation.Name}: new added symbol: {newType}");

            allNewSymbolsList.Add(symbol);
        }

        allNewSymbols = allNewSymbolsList;
        return;

        SymbolJsonResult ReadSymbolFromJsonFileResult(JsonFileResult<Symbol> jsonInfo)
        {
            var result = SymbolJson.ReadSymbolRoot(jsonInfo.Guid, jsonInfo.JToken, allowNonOperatorInstanceType: false, this);

            jsonInfo.Object = result.Symbol;
            return new SymbolJsonResult(result, jsonInfo.FilePath);
        }
    }

    public bool TryCreateInstance(Guid id, Guid instanceId, Instance parent, out Instance instance)
    {
        if (Symbols.TryGetValue(id, out var symbol))
        {
            instance = symbol.CreateInstance(instanceId, parent);
            return true;
        }

        instance = null;
        return false;
    }

    internal Symbol CreateSymbol(Type instanceType, Guid id, string name, string @namespace, Guid[] orderedInputIds = null)
    {
        var symbol = new Symbol(instanceType, id, this, orderedInputIds)
                         {
                             Name = name,
                             Namespace = @namespace,
                         };

        symbol.SymbolPackage = this;
        return symbol;
    }

    protected virtual bool RemoveSymbol(Guid guid)
    {
        var removed = Symbols.Remove(guid, out var symbol) && SymbolRegistry.EntriesEditable.Remove(guid, out _);
        if (removed)
        {
            OnSymbolRemoved(symbol);
        }

        return removed;
    }
    
    protected abstract void OnSymbolRemoved(Symbol symbol);

    public void ApplySymbolChildren(List<SymbolJson.SymbolReadResult> symbolsRead)
    {
        Log.Debug($"{AssemblyInformation.Name}: Applying symbol children...");
        Parallel.ForEach(symbolsRead, ReadAndApplyChildren);
        Log.Debug($"{AssemblyInformation.Name}: Done applying symbol children.");
        return;

        void ReadAndApplyChildren(SymbolJson.SymbolReadResult result)
        {
            if (!SymbolJson.TryReadAndApplySymbolChildren(result))
            {
                Log.Error($"Problem obtaining children of {result.Symbol.Name} ({result.Symbol.Id})");
            }
        }
    }

    public readonly record struct SymbolJsonResult(in SymbolJson.SymbolReadResult Result, string Path);

    private static readonly OpUpdateCounter _updateCounter;

    public abstract bool IsModifiable { get; }

    protected readonly Dictionary<Guid, Symbol> Symbols = new();

    public const string SymbolExtension = ".t3";

    public bool ContainsSymbolName(string newSymbolName) => Symbols.Values.Any(symbol => symbol.Name == newSymbolName);
}