using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;

namespace t3.Gui.Interaction.Presets.Model
{
    /// <summary>
    /// Collects all presets and variations for a symbol 
    /// </summary>
    public class SymbolVariationPool
    {
        public Guid SymbolId;
        public List<VariationModel> Variations;
        public List<VariationModel> Presets_;

        public static SymbolVariationPool InitVariationPoolForSymbol(Guid compositionId)
        {
            var newPool = new SymbolVariationPool()
                              {
                                  SymbolId = compositionId
                              };

            newPool.Variations = LoadVariations(compositionId, VariationType.SymbolVariation);
            newPool.Presets_ = LoadVariations(compositionId, VariationType.Preset);
            return newPool;
        }

        private enum VariationType
        {
            Preset,
            SymbolVariation,
        }

        private static List<VariationModel> LoadVariations(Guid compositionId, VariationType variationType)
        {
            var filepath = variationType == VariationType.SymbolVariation
                               ? $".t3/Variations/{compositionId}.var"
                               : $".Presets/{compositionId}.var";

            if (!File.Exists(filepath))
            {
                return new List<VariationModel>();
            }

            Log.Info($"Reading presets definition for : {compositionId}");

            using var sr = new StreamReader(filepath);
            using var jsonReader = new JsonTextReader(sr);

            var result = new List<VariationModel>();

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

                    var newVariation = VariationModel.FromJson(compositionId, sceneToken);
                    if (newVariation == null)
                    {
                        Log.Warning($"Failed to parse variation json:" + sceneToken);
                        continue;
                    }

                    newVariation.IsPreset = variationType == VariationType.Preset;
                    result.Add(newVariation);
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load presets and variations for {compositionId}: {e.Message}");
                return new List<VariationModel>();
            }

            return result;

        }

        private static string GetFilepathForCompositionId(Guid id)
        {
            return PresetFolderPath + GetFilenameForCompositionId(id);
        }

        public static string PresetFolderPath = ".Presets/";
        public static string UserVariationsFolderPath = ".t3/UserVariations/";

        private static string GetFilenameForCompositionId(Guid id)
        {
            return $"{id}_variations.json";
        }

        public void ApplyPreset(Instance instance, int presetIndex)
        {
            var preset = Presets_.FirstOrDefault(c => c.ActivationIndex == presetIndex);
            if (preset == null)
            {
                Log.Error($"Can't find preset with index {presetIndex}");
                return;
            }

            var commands = new List<ICommand>();
            var parentSymbol = instance.Parent.Symbol;

            if (preset.InputValuesForChildIds.TryGetValue(Guid.Empty, out var parametersForOp))
            {
                var symbolChild = parentSymbol.Children.SingleOrDefault(s => s.Id == instance.SymbolChildId);
                if (symbolChild != null)
                {
                    foreach (var (childId, parametersForInputs) in preset.InputValuesForChildIds)
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
    }
}