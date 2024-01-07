using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Stats;

// ReSharper disable RedundantNameQualifier

namespace T3.Core.Model;

public abstract partial class SymbolPackage
{
    protected abstract AssemblyInformation AssemblyInformation { get; }
    public abstract string Folder { get; }

    static SymbolPackage()
    {
        _updateCounter = new OpUpdateCounter();
        RegisterTypes();
    }

    public void LoadSymbols(bool enableLog, out List<SymbolJsonResult> symbolsRead)
    {
        Log.Debug($"{AssemblyInformation.Name}: Loading symbols...");
        var symbolFiles = Directory.EnumerateFiles(Folder, $"*{SymbolExtension}", SearchOption.AllDirectories);

        symbolsRead = symbolFiles.AsParallel()
                                 .Select(JsonFileResult<Symbol>.ReadAndCreate)
                                 .Select(ReadSymbolFromJsonFileResult)
                                 .Where(symbolReadResult => symbolReadResult.Result.Symbol is not null)
                                 .ToList(); // Execute and bring back to main thread

        // Check if there are symbols without a file, if yes add these. this is a copy
        var instanceTypesWithoutFile = AssemblyInformation.OperatorTypes.ToDictionary();

        Log.Debug($"{AssemblyInformation.Name}: Registering loaded symbols...");

        foreach (var readSymbolResult in symbolsRead)
        {
            var symbol = readSymbolResult.Result.Symbol;

            if (!TryAddSymbolTo(Symbols, symbol))
                continue;

            if (!TryAddSymbolTo(SymbolRegistry.Entries, symbol))
                continue;

            instanceTypesWithoutFile.Remove(symbol.Id);
            symbol.SymbolPackage = this;
            symbol.SymbolFilePath = readSymbolResult.Path;
        }

        foreach (var (guid, newType) in instanceTypesWithoutFile)
        {
            var registered = TryRegisterTypeWithoutFile(newType, guid, out var symbol);
            if (registered)
            {
                TryAddSymbolTo(Symbols, symbol);
                symbol.SymbolPackage = this;
            }
        }

        return;

        SymbolJsonResult ReadSymbolFromJsonFileResult(JsonFileResult<Symbol> jsonInfo)
        {
            var result = SymbolJson.ReadSymbolRoot(jsonInfo.Guid, jsonInfo.JToken, allowNonOperatorInstanceType: false, AssemblyInformation);

            jsonInfo.Object = result.Symbol;
            return new SymbolJsonResult(result, jsonInfo.FilePath);
        }


        bool TryRegisterTypeWithoutFile(Type newType, Guid guid, out Symbol symbol)
        {
            var typeNamespace = newType.Namespace;
            if (string.IsNullOrWhiteSpace(typeNamespace))
            {
                Log.Error($"Null or empty namespace of type {newType.Name}");
                symbol = null;
                return false;
            }

            var @namespace = newType.Namespace;

            if (string.IsNullOrWhiteSpace(@namespace))
            {
                // set namespace to assembly name
                @namespace = AssemblyInformation.Name;
            }

            symbol = CreateSymbol(newType, guid, @namespace);

            if (!TryAddSymbolTo(Symbols, symbol))
            {
                Log.Error($"Ignoring redefinition symbol {symbol.Name}. Please fix multiple definitions in Operators/Types/ folder");
                return false;
            }

            if (enableLog)
                Log.Debug($"new added symbol: {newType}");

            return true;
        }

        static bool TryAddSymbolTo(Dictionary<Guid, Symbol> collection, Symbol symbol)
        {
            bool added;
            lock (collection)
                added = collection.TryAdd(symbol.Id, symbol);

            if (!added)
            {
                var existingSymbol = collection[symbol.Id];
                Log.Error($"Symbol {existingSymbol.Name} {symbol.Id} exists multiple times in database.");
            }

            return added;
        }
    }

    protected static Symbol CreateSymbol(Type newType, Guid guid, string @namespace)
    {
        return new Symbol(newType, guid)
                     {
                         Namespace = @namespace,
                         Name = newType.Name
                     };
    }

    public void ApplySymbolChildren(List<SymbolJsonResult> symbolsRead)
    {
        Log.Debug($"{AssemblyInformation.Name}: Applying symbol children...");
        Parallel.ForEach(symbolsRead, ReadAndApplyChildren);
        Log.Debug($"{AssemblyInformation.Name}: Done applying symbol children.");
        return;

        void ReadAndApplyChildren(SymbolJsonResult readSymbolResult)
        {
            var result = readSymbolResult.Result;
            if (!SymbolJson.TryReadAndApplySymbolChildren(result))
            {
                Log.Error($"Problem obtaining children of {result.Symbol.Name} ({result.Symbol.Id})");
            }
        }
    }


    public virtual void AddSymbol(Symbol newSymbol)
    {
        SymbolRegistry.Entries.Add(newSymbol.Id, newSymbol);
        Symbols.Add(newSymbol.Id, newSymbol);
        AssemblyInformation.UpdateType(newSymbol.InstanceType, newSymbol.Id);
    }
    
    public readonly record struct SymbolJsonResult(in SymbolJson.SymbolReadResult Result, string Path);

    private static readonly OpUpdateCounter _updateCounter;

    public const string SourceCodeExtension = ".cs";
    public const string SymbolExtension = ".t3";
    public const string SymbolUiExtension = ".t3ui";

    public abstract bool IsModifiable { get; }

    protected readonly Dictionary<Guid, Symbol> Symbols = new();
}