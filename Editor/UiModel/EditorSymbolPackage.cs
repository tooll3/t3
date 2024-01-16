using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Editor.Gui.ChildUi;

namespace T3.Editor.UiModel;

// todo - make abstract, create NugetSymbolPackage
internal class EditorSymbolPackage : StaticSymbolPackage
{
    public EditorSymbolPackage(AssemblyInformation assembly) : base(assembly)
    {
    }

    public void LoadUiFiles(IEnumerable<Symbol> newlyReadSymbols, out IReadOnlyCollection<SymbolUi> newlyReadSymbolUis)
    {
        var newSymbols = newlyReadSymbols.ToDictionary(result => result.Id, symbol => symbol);
        var newSymbolsWithoutUis = new ConcurrentDictionary<Guid, Symbol>(newSymbols);
        Log.Debug($"{AssemblyInformation.Name}: Loading Symbol UIs from \"{Folder}\"");
        var newlyReadSymbolUiList = Directory.EnumerateFiles(Folder, $"*{SymbolUiExtension}", SearchOption.AllDirectories)
                                             .AsParallel()
                                             .Select(JsonFileResult<SymbolUi>.ReadAndCreate)
                                             .Where(result => newSymbols.ContainsKey(result.Guid))
                                             .Select(symbolUiJson =>
                                                     {
                                                         var gotSymbolUi =
                                                             SymbolUiJson.TryReadSymbolUi(symbolUiJson.JToken, symbolUiJson.Guid, out var symbolUi);
                                                         if (!gotSymbolUi)
                                                         {
                                                             Log.Error($"Error reading symbol Ui for {symbolUiJson.Guid} from file \"{symbolUiJson.FilePath}\"");
                                                             return null;
                                                         }

                                                         symbolUi.UiFilePath = symbolUiJson.FilePath;
                                                         symbolUiJson.Object = symbolUi;
                                                         return symbolUiJson;
                                                     })
                                             .Where(result =>
                                                    {
                                                        if (result?.Object == null)
                                                            return false;

                                                        var symbolUi = result.Object;
                                                        newSymbolsWithoutUis.Remove(symbolUi.Symbol.Id, out _);
                                                        return SymbolUis.TryAdd(symbolUi.Symbol.Id, symbolUi);
                                                    })
                                             .Select(result => result!.Object)
                                             .ToList();

        foreach (var (guid, symbol) in newSymbolsWithoutUis)
        {
            var symbolUi = new SymbolUi(symbol);

            if (!SymbolUis.TryAdd(guid, symbolUi))
            {
                Log.Error($"{AssemblyInformation.Name}: Duplicate symbol UI for {symbol.Name}?");
                continue;
            }

            newlyReadSymbolUiList.Add(symbolUi);
        }

        newlyReadSymbolUis = newlyReadSymbolUiList;
    }

    private static void RegisterCustomChildUi(Symbol symbol)
    {
        var valueInstanceType = symbol.InstanceType;
        if (typeof(IDescriptiveFilename).IsAssignableFrom(valueInstanceType))
        {
            CustomChildUiRegistry.Entries.TryAdd(valueInstanceType, DescriptiveUi.DrawChildUi);
        }
    }

    public void RegisterUiSymbols(bool enableLog, IEnumerable<SymbolUi> newSymbolUis)
    {
        Log.Debug($@"{AssemblyInformation.Name}: Registering UI entries...");

        foreach (var symbolUi in newSymbolUis)
        {
            var symbol = symbolUi.Symbol;

            RegisterCustomChildUi(symbol);

            if (!SymbolUiRegistry.EntriesEditable.TryAdd(symbol.Id, symbolUi))
            {
                SymbolUis.Remove(symbol.Id, out _);
                Log.Error($"Can't load UI for [{symbolUi.Symbol.Name}] Registry already contains id {symbolUi.Symbol.Id}.");
                continue;
            }

            symbolUi.UpdateConsistencyWithSymbol();
            if (enableLog)
                Log.Debug($"Add UI for {symbolUi.Symbol.Name} {symbolUi.Symbol.Id}");
        }
    }

    protected override bool RemoveSymbol(Guid guid)
    {
        return base.RemoveSymbol(guid)
               && SymbolUis.Remove(guid, out _)
               && SymbolUiRegistry.EntriesEditable.Remove(guid, out _);
    }

    protected readonly ConcurrentDictionary<Guid, SymbolUi> SymbolUis = new();
    protected override string ResourcesSubfolder => "Resources";

    public override bool IsModifiable => false;
    protected const string SymbolUiExtension = ".t3ui";
}