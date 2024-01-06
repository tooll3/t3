using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

public abstract partial class SymbolPackage
{
    public AssemblyInformation AssemblyInformation { get; }
    public abstract string Folder { get; }

    static SymbolPackage()
    {
        _updateCounter = new OpUpdateCounter();
        RegisterTypes();
    }

    protected SymbolPackage(AssemblyInformation assembly)
    {
        AssemblyInformation = assembly;
    }

    public void LoadSymbols(bool enableLog, out List<SymbolJson.SymbolReadResult> symbolsRead)
    {
        Log.Debug($"{AssemblyInformation.Name}: Loading symbols...");
        var symbolFiles = Directory.EnumerateFiles(Folder, $"*{SymbolExtension}", SearchOption.AllDirectories);

        symbolsRead = symbolFiles.AsParallel()
                                 .Select(JsonFileResult<Symbol>.ReadAndCreate)
                                 .Select(ReadSymbolFromJsonFileResult)
                                 .Where(symbolReadResult => symbolReadResult.Symbol is not null)
                                 .ToList(); // Execute and bring back to main thread

        // Check if there are symbols without a file, if yes add these
        var instanceTypesWithoutFile = AssemblyInformation.Assembly.ExportedTypes
                                                          .Where(type => type.IsSubclassOf(typeof(Instance)))
                                                          .Where(type => !type.IsGenericType)
                                                          .ToHashSet();

        Log.Debug($"{AssemblyInformation.Name}: Registering loaded symbols...");

        foreach (var readSymbolResult in symbolsRead)
        {
            var symbol = readSymbolResult.Symbol;

            if (!TryAddSymbolTo(Symbols, symbol))
                continue;

            if (!TryAddSymbolTo(SymbolRegistry.Entries, symbol))
                continue;

            instanceTypesWithoutFile.Remove(symbol.InstanceType);
            symbol.SymbolPackage = this;
        }

        foreach (var newType in instanceTypesWithoutFile)
        {
            var registered = TryRegisterTypeWithoutFile(newType, out var symbol);
            if (registered)
            {
                TryAddSymbolTo(Symbols, symbol);
                symbol.SymbolPackage = this;
            }
        }

        SymbolJson.SymbolReadResult ReadSymbolFromJsonFileResult(JsonFileResult<Symbol> jsonInfo)
        {
            var result = SymbolJson.ReadSymbolRoot(jsonInfo.Guid, jsonInfo.JToken, allowNonOperatorInstanceType: false, AssemblyInformation);

            jsonInfo.Object = result.Symbol;
            return result;
        }

        bool TryRegisterTypeWithoutFile(Type newType, out Symbol symbol)
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

            if (!TryGetGuidOfType(newType, out var guid))
            {
                symbol = null;
                return false;
            }

            symbol = new Symbol(newType, guid)
                         {
                             Namespace = @namespace,
                             Name = newType.Name
                         };

            var added = SymbolRegistry.Entries.TryAdd(symbol.Id, symbol);
            if (!added)
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

    public static bool TryGetGuidOfType(Type newType, out Guid guid)
    {
        var guidAttributes = newType.GetCustomAttributes(typeof(GuidAttribute), false);
        if(guidAttributes.Length == 0)
        {
            Log.Error($"Type {newType.Name} has no GuidAttribute");
            guid = Guid.Empty;
            return false;
        }
            
        if(guidAttributes.Length > 1)
        {
            Log.Error($"Type {newType.Name} has multiple GuidAttributes");
            guid = Guid.Empty;
            return false;
        }
            
        var guidAttribute = (GuidAttribute)guidAttributes[0];
        var guidString = guidAttribute.Value;
            
        if (!Guid.TryParse(guidString, out guid))
        {
            Log.Error($"Type {newType.Name} has invalid GuidAttribute");
            return false;
        }

        return true;
    }

    public void ApplySymbolChildren(List<SymbolJson.SymbolReadResult> symbolsRead)
    {
        Log.Debug($"{AssemblyInformation.Name}: Applying symbol children...");
        Parallel.ForEach(symbolsRead, ReadAndApplyChildren);
        Log.Debug($"{AssemblyInformation.Name}: Done applying symbol children.");
        return;

        void ReadAndApplyChildren(SymbolJson.SymbolReadResult readSymbolResult)
        {
            var gotSymbolChildren = SymbolJson.TryReadAndApplySymbolChildren(readSymbolResult);
            if (!gotSymbolChildren)
            {
                Log.Error($"Problem obtaining children of {readSymbolResult.Symbol.Name} ({readSymbolResult.Symbol.Id})");
            }
        }
    }


    public virtual void AddSymbol(Symbol newSymbol)
    {
        SymbolRegistry.Entries.Add(newSymbol.Id, newSymbol);
        Symbols.Add(newSymbol.Id, newSymbol);
        AssemblyInformation.UpdateType(newSymbol.InstanceType, newSymbol.Id);
    }

    private static readonly OpUpdateCounter _updateCounter;

    public const string SourceCodeExtension = ".cs";
    public const string SymbolExtension = ".t3";
    public const string SymbolUiExtension = ".t3ui";

    public static bool IsSaving => Interlocked.Read(ref _savingCount) > 0;
    public abstract bool IsModifiable { get; }

    private static long _savingCount;

    protected readonly Dictionary<Guid, Symbol> Symbols = new();
}