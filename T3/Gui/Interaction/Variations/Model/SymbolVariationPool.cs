using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using t3.Gui.Commands.Variations;
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
            var filepath = GetFilePathForVariationId(compositionId);

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
                var jArray = (JArray)jToken["Variations"];
                if (jArray != null)
                {
                    foreach (var sceneToken in jArray)
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
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load presets and variations for {compositionId}: {e.Message}");
                return new List<Variation>();
            }

            return result;
        }

        private void SaveVariations()
        {
            if (Variations.Count == 0)
                return;

            var filePath = GetFilePathForVariationId(SymbolId);

            //Log.Info($"Reading presets definition for : {compositionId}");

            using var sw = new StreamWriter(filePath);
            using var writer = new JsonTextWriter(sw);

            try
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartObject();

                writer.WriteValue("Id", SymbolId);

                // Presets
                {
                    // writer.WriteValue("GroupCount", Presets.GetLength(0));
                    // writer.WriteValue("SceneCount", Presets.GetLength(1));
                    writer.WritePropertyName("Variations");
                    writer.WriteStartArray();
                    // for (var groupIndex = 0; groupIndex < Presets.GetLength(0); groupIndex++)
                    // {
                    //     for (var sceneIndex = 0; sceneIndex < Presets.GetLength(1); sceneIndex++)
                    //     {
                    //         writer.WriteStartObject();
                    //         writer.WriteComment($"preset {groupIndex}:{sceneIndex}");
                    //         var address = new PresetAddress(groupIndex, sceneIndex);
                    //         var preset = TryGetPresetAt(address);
                    //         preset?.ToJson(writer);
                    //
                    //         writer.WriteEndObject();
                    //     }
                    // }

                    writer.WriteEndArray();
                }

                // // Groups
                // {
                //     writer.WritePropertyName("Groups");
                //     writer.WriteStartArray();
                //     foreach (var @group in Groups)
                //     {
                //         writer.WriteStartObject();
                //         @group?.ToJson(writer);
                //         writer.WriteEndObject();
                //     }
                //
                //     writer.WriteEndArray();
                // }

                // // Scenes
                // {
                //     writer.WritePropertyName("Scenes");
                //     writer.WriteStartArray();
                //     foreach (var scene in Scenes)
                //     {
                //         writer.WriteStartObject();
                //         scene?.ToJson(writer);
                //         writer.WriteEndObject();
                //     }
                //
                //     writer.WriteEndArray();
                // }
                writer.WriteEndObject();
            }
            catch (Exception e)
            {
                Log.Error($"Saving variations failed: {e.Message}");
            }

            // using var sr = new StreamReader(filepath);
            // using var jsonReader = new JsonTextReader(sr);
            //
            // var result = new List<Variation>();
            //
            // try
            // {
            //     var jToken = JToken.ReadFrom(jsonReader);
            //     foreach (var sceneToken in (JArray)jToken["Variations"])
            //     {
            //         if (sceneToken == null)
            //         {
            //             Log.Error("No variations?");
            //             continue;
            //         }
            //
            //         var newVariation = Variation.FromJson(compositionId, sceneToken);
            //         if (newVariation == null)
            //         {
            //             Log.Warning($"Failed to parse variation json:" + sceneToken);
            //             continue;
            //         }
            //
            //         //TODO: this needs to be implemented
            //         newVariation.IsPreset = true;
            //         result.Add(newVariation);
            //     }
            // }
            // catch (Exception e)
            // {
            //     Log.Error($"Failed to load presets and variations for {compositionId}: {e.Message}");
            //     return new List<Variation>();
            // }
            //
            // return result;
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
            
            var command = new MacroCommand("Set Preset Values", commands);
            UndoRedoStack.AddAndExecute(command);
        }

        /// <summary>
        /// Save non-default parameters of single selected Instance as preset for its Symbol.  
        /// </summary>
        public void CreatePresetOfInstanceSymbol(Instance instance)
        {
            var changes = new Dictionary<Guid, InputValue>();

            foreach (var input in instance.Inputs)
            {
                if (input.Input.IsDefault)
                {
                    continue;
                }

                if (input.Input.Value is InputValue<float> floatValue)
                {
                    changes[input.Id] = floatValue;
                }
            }

            if (changes.Count == 0)
            {
                Log.Warning("All values are default. Nothing to save in preset");
                return;
            }

            var newVariation = new Variation
                                   {
                                       Id = Guid.NewGuid(),
                                       Title = "untitled",
                                       ActivationIndex = Variations.Count + 1, //TODO: First find the highest activation index
                                       IsPreset = true,
                                       PublishedDate = DateTime.Now,
                                       InputValuesForChildIds = new Dictionary<Guid, Dictionary<Guid, InputValue>>
                                                                    {
                                                                        [Guid.Empty] = changes
                                                                    },
                                   };

            var command = new AddPresetOrVariationCommand(instance.Symbol, newVariation);
            UndoRedoStack.AddAndExecute(command);
            SaveVariations();
        }

        private static string GetFilePathForVariationId(Guid compositionId)
        {
            var filepath = $".Variations/{compositionId}.var";
            return filepath;
        }
    }
}