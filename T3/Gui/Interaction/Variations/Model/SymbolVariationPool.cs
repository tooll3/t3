using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
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

                if (BlendMethods.ContainsKey(input.Input.Value.ValueType))
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

        private static string GetFilePathForVariationId(Guid compositionId)
        {
            var filepath = $".Variations/{compositionId}.var";
            return filepath;
        }

        public static readonly Dictionary<Type, Func<InputValue, InputValue, float, InputValue>> BlendMethods =
            new()
                {
                    { typeof(float), (a, b, t) =>
                                                 {
                                                     if (a is not InputValue<float> aValue || b is not InputValue<float> bValue)
                                                         return null;
                                                     
                                                     var r= MathUtils.Lerp(aValue.Value, bValue.Value, t);
                                                     return new InputValue<float>(r);
                                                 } },
                    { typeof(Vector2), (a, b, t) =>
                                                 {
                                                     if (a is not InputValue<Vector2> aValue || b is not InputValue<Vector2> bValue)
                                                         return null;
                                                     
                                                     var r= MathUtils.Lerp(aValue.Value, bValue.Value, t);
                                                     return new InputValue<Vector2>(r);
                                                 } },
                    { typeof(Vector3), (a, b, t) =>
                                                   {
                                                       if (a is not InputValue<Vector3> aValue || b is not InputValue<Vector3> bValue)
                                                           return null;
                                                     
                                                       var r= MathUtils.Lerp(aValue.Value, bValue.Value, t);
                                                       return new InputValue<Vector3>(r);
                                                   } },
                    { typeof(Vector4), (a, b, t) =>
                                                   {
                                                       if (a is not InputValue<Vector4> aValue || b is not InputValue<Vector4> bValue)
                                                           return null;
                                                     
                                                       var r= MathUtils.Lerp(aValue.Value, bValue.Value, t);
                                                       return new InputValue<Vector4>(r);
                                                   } },
                    
                    { typeof(Quaternion), (a, b, t) =>
                                                   {
                                                       if (a is not InputValue<Quaternion> aValue || b is not InputValue<Quaternion> bValue)
                                                           return null;
                                                     
                                                       var r= Quaternion.Slerp(aValue.Value, bValue.Value, t);
                                                       return new InputValue<Quaternion>(r);
                                                   } },                    
                    { typeof(int), (a, b, t) =>
                                                   {
                                                       if (a is not InputValue<int> aValue || b is not InputValue<int> bValue)
                                                           return null;
                                                     
                                                       var r= MathUtils.Lerp(aValue.Value, bValue.Value, t);
                                                       return new InputValue<int>(r);
                                                   } },
                    
                    { typeof(SharpDX.Int3), (a, b, t) =>
                                               {
                                                   if (a is not InputValue<SharpDX.Int3> aValue || b is not InputValue<SharpDX.Int3> bValue)
                                                       return null;
                                                     
                                                   var r= new SharpDX.Int3(MathUtils.Lerp(aValue.Value.X, bValue.Value.X, t), 
                                                                           MathUtils.Lerp(aValue.Value.Y, bValue.Value.Y, t),
                                                                           MathUtils.Lerp(aValue.Value.Z, bValue.Value.Z, t)
                                                                           );
                                                   return new InputValue<SharpDX.Int3>(r);
                                               } },
                    
                    { typeof(SharpDX.Size2), (a, b, t) =>
                                                        {
                                                            if (a is not InputValue<SharpDX.Size2> aValue || b is not InputValue<SharpDX.Size2> bValue)
                                                                return null;
                                                     
                                                            var r= new SharpDX.Size2(MathUtils.Lerp(aValue.Value.Width, bValue.Value.Width, t), 
                                                                                    MathUtils.Lerp(aValue.Value.Height, bValue.Value.Height, t)
                                                                                   );
                                                            return new InputValue<SharpDX.Size2>(r);
                                                        } },                    
                    
                };
    }
}