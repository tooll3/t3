using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Core.Resource;
using T3.Editor.Compilation;
using T3.Editor.Gui.ChildUi;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.InputUi.CombinedInputs;
using T3.Editor.Gui.InputUi.SimpleInputUis;
using T3.Editor.Gui.InputUi.SingleControl;
using T3.Editor.Gui.InputUi.VectorInputs;
using T3.Editor.Gui.OutputUi;
using Buffer = SharpDX.Direct3D11.Buffer;
using Point = T3.Core.DataTypes.Point;

// ReSharper disable RedundantNameQualifier

namespace T3.Editor.Gui
{

    public partial class UiModel : Model
    {
        public UiModel(Assembly operatorAssembly)
            : base(operatorAssembly)
        {
            Init();
        }

        private void Init()
        {
            foreach (var symbolEntry in SymbolRegistry.Entries)
            {
                var valueInstanceType = symbolEntry.Value.InstanceType;
                if (typeof(IDescriptiveFilename).IsAssignableFrom(valueInstanceType))
                {
                    CustomChildUiRegistry.Entries.Add(valueInstanceType, DescriptiveUi.DrawChildUi);
                }
            }

            Load(enableLog: false);

            var symbols = SymbolRegistry.Entries;
            foreach (var symbolEntry in symbols)
            {
                UpdateUiEntriesForSymbol(symbolEntry.Value);
            }

            // create instance of project op, all children are create automatically
            
            var homeSymbol = symbols[HomeSymbolId];
            
            Guid homeInstanceId = Guid.Parse("12d48d5a-b8f4-4e08-8d79-4438328662f0");
            RootInstance = homeSymbol.CreateInstance(homeInstanceId);
        }

        public static Guid HomeSymbolId = Guid.Parse("dab61a12-9996-401e-9aa6-328dd6292beb");
        
        public override void Load(bool enableLog)
        {
            // first load core data
            base.Load(enableLog);

            var symbolUiFiles = Directory.GetFiles(OperatorTypesFolder, $"*{SymbolUiExtension}", SearchOption.AllDirectories);
            var symbolUiJsons = symbolUiFiles.AsParallel()
                                         .Select(JsonFileResult<SymbolUi>.ReadAndCreate)
                                         .Select(symbolUiJson =>
                                                  {
                                                      var gotSymbolUi = SymbolUiJson.TryReadSymbolUi(symbolUiJson.JToken, symbolUiJson.Guid, out var symbolUi);
                                                      if (!gotSymbolUi)
                                                      {
                                                          Log.Error($"Error reading symbol Ui for {symbolUiJson.Guid} from file \"{symbolUiJson.FilePath}\"");
                                                          return null;
                                                      }

                                                      symbolUiJson.Object = symbolUi;
                                                      return symbolUiJson;
                                                  })
                                         .Where(x => x.ObjectWasSet)
                                         .ToList();

            foreach (var symbolUiJson in symbolUiJsons)
            {
                var symbolUi = symbolUiJson.Object;
                if (!SymbolUiRegistry.Entries.TryAdd(symbolUi.Symbol.Id, symbolUi))
                {
                    Log.Error($"Can't load UI for [{symbolUi.Symbol.Name}] Registry already contains id {symbolUi.Symbol.Id}.");
                    continue;
                }
                
                if(enableLog)
                    Log.Debug($"Add UI for {symbolUi.Symbol.Name} {symbolUi.Symbol.Id}");
            }
        }

        public override void SaveAll()
        {
            Log.Debug("Saving...");
            IsSaving = true;

            // Save all t3 and source files
            base.SaveAll();

            // Remove all old ui files before storing to get rid off invalid ones
            // TODO: this also seems dangerous, similar to how the Symbol SaveAll works
            var symbolUiFiles = Directory.GetFiles(OperatorTypesFolder, $"*{SymbolUiExtension}", SearchOption.AllDirectories);            
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

            WriteSymbolUis(SymbolUiRegistry.Entries.Values);

            IsSaving = false;
        }

        public static IEnumerable<SymbolUi> GetModifiedSymbolUis()
        {
            return SymbolUiRegistry.Entries.Values.Where(symbolUi => symbolUi.HasBeenModified);
        }

        /// <summary>
        /// Note: This does NOT clean up 
        /// </summary>
        public void SaveModifiedSymbols()
        {
            var modifiedSymbolUis = GetModifiedSymbolUis().ToList();
            Log.Debug($"Saving {modifiedSymbolUis.Count} modified symbols...");

            IsSaving = true;
            ResourceFileWatcher.DisableOperatorFileWatcher(); // Don't update ops if file is written during save
            
            var modifiedSymbols = modifiedSymbolUis.Select(symbolUi => symbolUi.Symbol).ToList();
            SaveSymbolDefinitionAndSourceFiles(modifiedSymbols);
            WriteSymbolUis(modifiedSymbolUis);
            
            ResourceFileWatcher.EnableOperatorFileWatcher();
            IsSaving = false;
        }

        private static void WriteSymbolUis(IEnumerable<SymbolUi> symbolUis)
        {
            var resourceManager = ResourceManager.Instance();

            foreach (var symbolUi in symbolUis)
            {
                var symbol = symbolUi.Symbol;
                var filepath = BuildFilepathForSymbol(symbol, SymbolUiExtension);

                using (var sw = new StreamWriter(filepath))
                using (var writer = new JsonTextWriter(sw))
                {
                    writer.Formatting = Formatting.Indented;
                    SymbolUiJson.WriteSymbolUi(symbolUi, writer);
                }

                var symbolSourceFilepath = BuildFilepathForSymbol(symbol, Model.SourceExtension);
                var opResource = resourceManager.GetOperatorFileResource(symbolSourceFilepath);
                if (opResource == null)
                {
                    // If the source wasn't registered before do this now
                    resourceManager.CreateOperatorEntry(symbolSourceFilepath, symbol.Id.ToString(), OperatorUpdating.ResourceUpdateHandler);
                }

                symbolUi.ClearModifiedFlag();
            }
        }

        public bool IsSaving { get; private set; }

        public static void UpdateUiEntriesForSymbol(Symbol symbol)
        {
            if (SymbolUiRegistry.Entries.TryGetValue(symbol.Id, out var symbolUi))
            {
                symbolUi.UpdateConsistencyWithSymbol();
            }
            else
            {
                var newSymbolUi = new SymbolUi(symbol);
                SymbolUiRegistry.Entries.Add(symbol.Id, newSymbolUi);
            }
        }

        public Instance RootInstance { get; private set; }
    }
}