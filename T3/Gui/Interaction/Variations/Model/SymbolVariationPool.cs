using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using t3.Gui.Interaction.Presets.Model;

namespace t3.Gui.Interaction.Variations.Model
{
    /// <summary>
    /// Collects all presets and variations for a symbol 
    /// </summary>
    public class SymbolVariationPool
    {
        public Guid SymbolId;
        public List<Variation> Variations;

        public static SymbolVariationPool InitVariationPoolForSymbol(Guid compositionId)
        {
            var newPool = new SymbolVariationPool()
                              {
                                  SymbolId = compositionId
                              };

            newPool.Variations = LoadVariations(compositionId);
            return newPool;
        }

        private static List<Variation> LoadVariations(Guid compositionId)
        {
            var filepath = $".Variations/{compositionId}.var";

            if (!File.Exists(filepath))
            {
                return new List<Variation>();
            }

            Log.Info($"Reading presets definition for : {compositionId}");

            using var sr = new StreamReader(filepath);
            using var jsonReader = new JsonTextReader(sr);

            var result = new List<Variation>();

            try
            {
                var jToken = JToken.ReadFrom(jsonReader);
                foreach (var sceneToken in (JArray)jToken["Variations"])
                {
                    if (sceneToken == null)
                    {
                        Log.Error("No variations?");
                        continue;
                    }

                    var newVariation = Variation.FromJson(compositionId, sceneToken);
                    if (newVariation == null)
                    {
                        Log.Warning($"Failed to parse variation json:" + sceneToken);
                        continue;
                    }

                    //TODO: this needs to be implemented
                    newVariation.IsPreset = true;
                    result.Add(newVariation);
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load presets and variations for {compositionId}: {e.Message}");
                return new List<Variation>();
            }

            return result;

        }


        public void ApplyPreset(Instance instance, int variationIndex)
        {
            var variation = Variations.FirstOrDefault(c => c.ActivationIndex == variationIndex);
            if (variation == null)
            {
                Log.Error($"Can't find preset with index {variationIndex}");
                return;
            }

            var commands = new List<ICommand>();
            var parentSymbol = instance.Parent.Symbol;

            if (variation.InputValuesForChildIds.TryGetValue(Guid.Empty, out var parametersForOp))
            {
                var symbolChild = parentSymbol.Children.SingleOrDefault(s => s.Id == instance.SymbolChildId);
                if (symbolChild != null)
                {
                    foreach (var (childId, parametersForInputs) in variation.InputValuesForChildIds)
                    {
                        if (childId != Guid.Empty)
                        {
                            Log.Warning("Didn't export childId in preset");
                            continue;
                        }
                        
                        foreach (var (inputId, parameter) in parametersForInputs)
                        {
                            if (parameter == null)
                            {
                                continue;
                            }

                            var input = symbolChild.InputValues[inputId];
                            var newCommand = new ChangeInputValueCommand(parentSymbol, instance.SymbolChildId, input)
                                                 {
                                                     NewValue = parameter,
                                                 };
                            commands.Add(newCommand);
                        }
                    }
                }
            }

            var command = new MacroCommand("Set Preset Values", commands);
            UndoRedoStack.AddAndExecute(command);
        }

        public void CreatePreset(Instance instance)
        {
            // ToBe implemented
        }
    }
}