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
using t3.Gui.Commands.Variations;

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

        private void SaveVariationsToFile()
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
                        Log.Warning("Didn't expect childId in preset");
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

        public void BeginHoverPreset(Instance instance, int variationIndex)
        {
            var variation = Variations.FirstOrDefault(c => c.ActivationIndex == variationIndex);
            if (variation == null)
            {
                Log.Error($"Can't find preset with index {variationIndex}");
                return;
            }

            if (_activeBlendCommand != null)
            {
                Log.Error("Can't start blending while blending already in progress");
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
                        Log.Warning("Didn't expect childId in preset");
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

            _activeBlendCommand =new MacroCommand("Set Preset Values", commands);
            _activeBlendCommand.Do();
        }

        public void StopHover()
        {
            if (_activeBlendCommand == null)
            {
                Log.Error("Can't stop non existing blend command");
                return;
            }
            
            _activeBlendCommand.Undo();
            _activeBlendCommand = null;
        }

        public void ApplyHovered()
        {
            if (_activeBlendCommand == null)
            {
                Log.Error("Can't apply non existing blend command");
                return;
            }
            
            UndoRedoStack.Add(_activeBlendCommand);
            _activeBlendCommand = null;
        }
        
        

        private MacroCommand _activeBlendCommand = null;
        
        
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

                if (ValueUtils.BlendMethods.ContainsKey(input.Input.Value.ValueType))
                {
                    changes[input.Id] = input.Input.Value.Clone();
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
            SaveVariationsToFile();
        }



    }


}