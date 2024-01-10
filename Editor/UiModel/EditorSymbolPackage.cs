using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    public void LoadUiFiles(List<SymbolJson.SymbolReadResult> newlyReadSymbols, out IReadOnlyCollection<SymbolUi> newlyReadSymbolUis)
    {
        var newSymbols = newlyReadSymbols.ToDictionary(result => result.Symbol.Id, result => result.Symbol);
        Log.Debug($"{AssemblyInformation.Name}: Loading Symbol UIs from \"{Folder}\"");
        newlyReadSymbolUis = Directory.EnumerateFiles(Folder, $"*{SymbolUiExtension}", SearchOption.AllDirectories)
                                      .AsParallel()
                                      .Select(JsonFileResult<SymbolUi>.ReadAndCreate)
                                      .Where(result => newSymbols.ContainsKey(result.Guid))
                                      .Select(symbolUiJson =>
                                              {
                                                  var gotSymbolUi = SymbolUiJson.TryReadSymbolUi(symbolUiJson.JToken, symbolUiJson.Guid, out var symbolUi);
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
                                                 return SymbolUis.TryAdd(symbolUi.Symbol.Id, symbolUi);
                                             })
                                      .Select(result => result!.Object)
                                      .ToArray();

        Log.Debug($"{AssemblyInformation.Name}: Loaded {newlyReadSymbolUis.Count} symbol UIs");
    }

    protected static void RegisterCustomChildUi(Symbol symbol)
    {
        var valueInstanceType = symbol.InstanceType;
        if (typeof(IDescriptiveFilename).IsAssignableFrom(valueInstanceType))
        {
            CustomChildUiRegistry.Entries.TryAdd(valueInstanceType, DescriptiveUi.DrawChildUi);
        }
    }

    public void RegisterUiSymbols(bool enableLog, IReadOnlyCollection<SymbolUi> newSymbolUis)
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

    protected readonly ConcurrentDictionary<Guid, SymbolUi> SymbolUis = new();

    public override bool IsModifiable => false;
    protected const string SymbolUiExtension = ".t3ui";
}