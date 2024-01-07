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

internal class EditorSymbolPackage : StaticSymbolPackage
{
    public EditorSymbolPackage(AssemblyInformation assembly) : base(assembly)
    {
    }

    public void LoadUiFiles()
    {
        Log.Debug($"{AssemblyInformation.Name}: Loading Symbol UIs from \"{Folder}\"");
        var symbolUiFiles = Directory.EnumerateFiles(Folder, $"*{SymbolUiExtension}", SearchOption.AllDirectories);
        SymbolUis = symbolUiFiles.AsParallel()
                                  .Select(JsonFileResult<SymbolUi>.ReadAndCreate)
                                  .Select(symbolUiJson =>
                                          {
                                              var gotSymbolUi = SymbolUiJson.TryReadSymbolUi(symbolUiJson.JToken, symbolUiJson.Guid, out var symbolUi);
                                              if (!gotSymbolUi)
                                              {
                                                  Log.Error($"Error reading symbol Ui for {symbolUiJson.Guid} from file \"{symbolUiJson.FilePath}\"");
                                                  return null;
                                              }

                                              symbolUi.FilePath = symbolUiJson.FilePath;
                                              symbolUiJson.Object = symbolUi;
                                              return symbolUiJson;
                                          })
                                  .Where(result => result?.Object != null)
                                  .Select(result => result.Object)
                                  .ToList();

        Log.Debug($"{AssemblyInformation.Name}: Loaded {SymbolUis.Count} symbol UIs");
    }

    protected static void RegisterCustomChildUi(Symbol symbol)
    {
        var valueInstanceType = symbol.InstanceType;
        if (typeof(IDescriptiveFilename).IsAssignableFrom(valueInstanceType))
        {
            CustomChildUiRegistry.Entries.TryAdd(valueInstanceType, DescriptiveUi.DrawChildUi);
        }
    }
    
    public void RegisterUiSymbols(bool enableLog)
    {
        Log.Debug($@"{AssemblyInformation.Name}: Registering UI entries...");

        foreach (var symbolUi in SymbolUis)
        {
            var symbol = symbolUi.Symbol;

            RegisterCustomChildUi(symbol);

            if (!SymbolUiRegistry.Entries.TryAdd(symbol.Id, symbolUi))
            {
                Log.Error($"Can't load UI for [{symbolUi.Symbol.Name}] Registry already contains id {symbolUi.Symbol.Id}.");
                continue;
            }

            symbolUi.UpdateConsistencyWithSymbol();
            if (enableLog)
                Log.Debug($"Add UI for {symbolUi.Symbol.Name} {symbolUi.Symbol.Id}");
        }
    }

    protected List<SymbolUi> SymbolUis = new();
    
    public override bool IsModifiable => false;
}