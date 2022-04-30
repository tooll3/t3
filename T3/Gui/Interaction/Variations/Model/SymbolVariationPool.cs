using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.Resource;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Commands.Variations;

namespace T3.Gui.Interaction.Variations.Model
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

        #region serialization
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

        public void SaveVariationsToFile()
        {
            if (Variations.Count == 0)
                return;

            var filePath = GetFilePathForVariationId(SymbolId);

            using var sw = new StreamWriter(filePath);
            using var writer = new JsonTextWriter(sw);

            try
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartObject();

                writer.WriteValue("Id", SymbolId);

                // Presets
                {
                    writer.WritePropertyName("Variations");
                    writer.WriteStartArray();
                    foreach (var v in Variations)
                    {
                        v.ToJson(writer);
                    }

                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }
            catch (Exception e)
            {
                Log.Error($"Saving variations failed: {e.Message}");
            }
        }

        private static string GetFilePathForVariationId(Guid compositionId)
        {
            var filepath = $".Variations/{compositionId}.var";
            return filepath;
        }
        #endregion

        public void ApplyPreset(Instance instance, Variation variation, bool resetOtherNonDefaults = false)
        {
            StopHover();
            
            var command = CreateApplyCommand(instance, variation, resetOtherNonDefaults);
            UndoRedoStack.AddAndExecute(command);
        }

        public void BeginHoverPreset(Instance instance, Variation variation, bool resetNonDefaults)
        {
            StopHover();
            
            _activeBlendCommand = CreateApplyCommand(instance, variation, resetNonDefaults);
            _activeBlendCommand.Do();
        }

        public void BeginBlendToPresent(Instance instance, Variation variation, float blend, bool resetToDefaults)
        {
            StopHover();

            _activeBlendCommand = CreateBlendToCommand(instance, variation, blend, resetToDefaults);
            _activeBlendCommand.Do();
        }

        public void BeginWeightedBlend(Instance instance, List<Variation> variations, IEnumerable<float> weights, bool resetToDefaults)
        {
            StopHover();

            _activeBlendCommand = CreateWeightedBlendCommand(instance, variations, weights, resetToDefaults);
            _activeBlendCommand.Do();
        }

        public void ApplyCurrentBlend()
        {
            UndoRedoStack.Add(_activeBlendCommand);
            _activeBlendCommand = null;
        }

        
        public void StopHover()
        {
            if (_activeBlendCommand == null)
            {
                return;
            }

            _activeBlendCommand.Undo();
            _activeBlendCommand = null;
        }

        /// <summary>
        /// Save non-default parameters of single selected Instance as preset for its Symbol.  
        /// </summary>
        public Variation CreatePresetOfInstanceSymbol(Instance instance)
        {
            var changes = new Dictionary<Guid, InputValue>();

            foreach (var input in instance.Inputs)
            {
                if (input.Input.IsDefault)
                {
                    continue;
                }

                if (ValueUtils.BlendMethods.ContainsKey(input.Input.Value.ValueType))
                {
                    changes[input.Id] = input.Input.Value.Clone();
                }
            }

            if (changes.Count == 0)
            {
                Log.Warning("All values are default. Nothing to save in preset");
                return null;
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
            SaveVariationsToFile();
            return newVariation;
        }

        public void DeleteVariation(Variation variation)
        {
            var command = new DeleteVariationCommand(this, variation);
            UndoRedoStack.AddAndExecute(command);
            SaveVariationsToFile();
        }

        public void DeleteVariations(List<Variation> variations)
        {
            var commands = new List<ICommand>();
            foreach (var variation in variations)
            {
                commands.Add(new DeleteVariationCommand(this, variation));
            }

            var newCommand = new MacroCommand("Delete variations", commands);
            UndoRedoStack.AddAndExecute(newCommand);
            SaveVariationsToFile();
        }

        private static MacroCommand CreateApplyCommand(Instance instance, Variation variation, bool resetOtherNonDefaults)
        {
            var commands = new List<ICommand>();
            var parentSymbol = instance.Parent.Symbol;

            var symbolChild = parentSymbol.Children.SingleOrDefault(s => s.Id == instance.SymbolChildId);
            if (symbolChild != null)
            {
                foreach (var (childId, parametersForInputs) in variation.InputValuesForChildIds)
                {
                    if (childId != Guid.Empty)
                    {
                        Log.Warning("Didn't expect childId in preset");
                        continue;
                    }

                    foreach (var inputSlot in instance.Inputs)
                    {
                        if (!ValueUtils.BlendMethods.TryGetValue(inputSlot.ValueType, out var blendFunction))
                            continue;
                        
                        if (parametersForInputs.TryGetValue(inputSlot.Id, out var param))
                        {
                            if (param == null)
                                continue;

                            var newCommand = new ChangeInputValueCommand(parentSymbol, instance.SymbolChildId, inputSlot.Input)
                                                 {
                                                     NewValue = param,
                                                 };
                            commands.Add(newCommand);
                        }
                        else
                        {
                            if (resetOtherNonDefaults)
                            {
                                commands.Add(new ResetInputToDefault(instance.Parent.Symbol, instance.SymbolChildId, inputSlot.Input));
                            }
                        }
                    }
                }
            }

            var command = new MacroCommand("Apply Preset Values", commands);
            return command;
        }

        private static MacroCommand CreateBlendToCommand(Instance instance, Variation variation, float blend, bool resetToDefaults)
        {
            var commands = new List<ICommand>();
            var parentSymbol = instance.Parent.Symbol;

            var symbolChild = parentSymbol.Children.SingleOrDefault(s => s.Id == instance.SymbolChildId);
            if (symbolChild != null)
            {
                foreach (var (childId, parametersForInputs) in variation.InputValuesForChildIds)
                {
                    if (childId != Guid.Empty)
                    {
                        Log.Warning("Didn't expect childId in preset");
                        continue;
                    }

                    foreach (var inputSlot in instance.Inputs)
                    {
                        if (!ValueUtils.BlendMethods.TryGetValue(inputSlot.Input.DefaultValue.ValueType, out var blendFunction))
                            continue;

                        if (parametersForInputs.TryGetValue(inputSlot.Id, out var parameter))
                        {
                            if (parameter == null)
                                continue;

                            var mixed = blendFunction(inputSlot.Input.Value, parameter, blend);
                            var newCommand = new ChangeInputValueCommand(parentSymbol, instance.SymbolChildId, inputSlot.Input)
                                                 {
                                                     NewValue = mixed,
                                                 };
                            commands.Add(newCommand);
                        }
                        else if (!inputSlot.Input.IsDefault && resetToDefaults)
                        {
                            var mixed = blendFunction(inputSlot.Input.Value, inputSlot.Input.DefaultValue, blend);
                            var newCommand = new ChangeInputValueCommand(parentSymbol, instance.SymbolChildId, inputSlot.Input)
                                                 {
                                                     NewValue = mixed,
                                                 };
                            commands.Add(newCommand);
                        }
                    }
                }
            }

            var activeBlendCommand = new MacroCommand("Set Preset Values", commands);
            return activeBlendCommand;
        }
        
        private static MacroCommand CreateWeightedBlendCommand(Instance instance, List<Variation> variations, IEnumerable<float> weights, bool resetToDefaults)
        {
            var commands = new List<ICommand>();
            var parentSymbol = instance.Parent.Symbol;
            var weightsArray = weights.ToArray();
            
            var symbolChild = parentSymbol.Children.SingleOrDefault(s => s.Id == instance.SymbolChildId);
            if (symbolChild != null)
            {
                // collect variation parameters
                var variationParameters = new List<Dictionary<Guid, InputValue>>();
                foreach (var variation in variations)
                {
                    foreach (var (childId, parametersForInputs) in variation.InputValuesForChildIds)
                    {
                        if (childId != Guid.Empty)
                        {
                            Log.Warning("Didn't expect childId in preset");
                            continue;
                        }

                        variationParameters.Add(parametersForInputs);
                    }
                }


                foreach (var inputSlot in instance.Inputs)
                {
                    if (!ValueUtils.WeightedBlendMethods.TryGetValue(inputSlot.Input.DefaultValue.ValueType, out var blendFunction))
                        continue;

                    var values = new List<InputValue>();
                    
                    foreach (var parametersForInputs in variationParameters)
                    {
                        if (parametersForInputs.TryGetValue(inputSlot.Id, out var parameterValue))
                        {
                            if (parameterValue == null)
                                continue;
                            
                            values.Add(parameterValue);                            
                        }
                        else if (!inputSlot.Input.IsDefault && resetToDefaults)
                        {
                            values.Add(inputSlot.Input.DefaultValue);
                        }
                    }

                    if (weightsArray.Length == values.Count)
                    {
                        var mixed2 = blendFunction(values.ToArray(), weightsArray);
                        var newCommand = new ChangeInputValueCommand(parentSymbol, instance.SymbolChildId, inputSlot.Input)
                                             {
                                                 NewValue = mixed2,
                                             };
                        commands.Add(newCommand);
                    }
                }
            }

            var activeBlendCommand = new MacroCommand("Set Blended Preset Values", commands);
            return activeBlendCommand;
        }
        

        private MacroCommand _activeBlendCommand;
    }
}