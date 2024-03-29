using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Commands.Variations;

namespace T3.Editor.Gui.Interaction.Variations.Model
{
    /// <summary>
    /// Collects all presets and variations for a symbol 
    /// </summary>
    public class SymbolVariationPool
    {
        public Guid SymbolId;
        public List<Variation> Variations;

        public Variation ActiveVariation { get; private set; }

        public static SymbolVariationPool InitVariationPoolForSymbol(Guid compositionId)
        {
            var newPool = new SymbolVariationPool
                              {
                                  SymbolId = compositionId,
                                  Variations = LoadVariations(compositionId)
                              };

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
                var jToken = JToken.ReadFrom(jsonReader, SymbolJson.LoadSettings);
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
                        //newVariation.IsPreset = true;
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
            // if (Variations.Count == 0)
            //     return;

            CreateFolderIfNotExists(VariationsFolder);
            
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

        private const string VariationsFolder = ".Variations";

        private static string GetFilePathForVariationId(Guid compositionId)
        {
            var filepath = Path.Combine(VariationsFolder, $"{compositionId}.var");
            return filepath;
        }

        private static void CreateFolderIfNotExists(string path)
        {
            if(Directory.Exists(path))
                return;

            Directory.CreateDirectory(path);

        }
        
        
        #endregion

        public void Apply(Instance instance, Variation variation)
        {
            StopHover();
            ActiveVariation = variation;

            var command = variation.IsPreset
                              ? CreateApplyPresetCommand(instance, variation)
                              : CreateApplyVariationCommand(instance, variation);
            UpdateActiveStateForVariation(variation.ActivationIndex);

            UndoRedoStack.AddAndExecute(command);
        }

        public void UpdateActiveStateForVariation(int variationIndex)
        {
            foreach (var v in Variations)
            {
                v.State = v.ActivationIndex == variationIndex ? Variation.States.Active : Variation.States.InActive;
            }

            //variation.State = Variation.States.Active;
        }

        public void BeginHover(Instance instance, Variation variation)
        {
            StopHover();

            _activeBlendCommand = variation.IsPreset
                                      ? CreateApplyPresetCommand(instance, variation)
                                      : CreateApplyVariationCommand(instance, variation);
            _activeBlendCommand.Do();
        }

        public void BeginBlendToPresent(Instance instance, Variation variation, float blend)
        {
            StopHover();

            _activeBlendCommand = CreateBlendToPresetCommand(instance, variation, blend);
            _activeBlendCommand.Do();
        }
        
        public void BeginBlendTowardsSnapshot(Instance instance, Variation variation, float blend)
        {
            StopHover();

            _activeBlendCommand = CreateBlendTowardsVariationCommand(instance, variation, blend);
            _activeBlendCommand.Do();
            UpdateActiveStateForVariation(variation.ActivationIndex);
        }
        

        public void BeginWeightedBlend(Instance instance, List<Variation> variations, IEnumerable<float> weights)
        {
            StopHover();

            if (variations.Count == 0)
                return;

            var countPresets = 0;
            var countSnapshots = 0;
            foreach (var s in variations)
            {
                if (s.IsPreset)
                {
                    countPresets++;
                }
                else
                {
                    countSnapshots++;
                }
            }

            if (countSnapshots == variations.Count && countPresets == 0)
            {
                _activeBlendCommand = CreateWeightedBlendSnapshotCommand(instance, variations, weights);
                _activeBlendCommand?.Do();
            }
            else if (countPresets == variations.Count && countSnapshots == 0)
            {
                _activeBlendCommand = CreateWeightedBlendPresetCommand(instance, variations, weights);
                _activeBlendCommand?.Do();
            }
            else
            {
                Log.Error($"Can't mix {countPresets} presets and {countSnapshots} snapshots for weighted blending.");
            }
        }

        public void ApplyCurrentBlend()
        {
            if(_activeBlendCommand != null)
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
        public Variation CreatePresetForInstanceSymbol(Instance instance)
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
            
            var newVariation = new Variation
                                   {
                                       Id = Guid.NewGuid(),
                                       Title = "untitled",
                                       ActivationIndex = Variations.Count + 1, //TODO: First find the highest activation index
                                       IsPreset = true,
                                       PublishedDate = DateTime.Now,
                                       ParameterSetsForChildIds = new Dictionary<Guid, Dictionary<Guid, InputValue>>
                                                                      {
                                                                          [Guid.Empty] = changes
                                                                      },
                                   };

            var command = new AddPresetOrVariationCommand(instance.Symbol, newVariation);
            UndoRedoStack.AddAndExecute(command);
            //SaveVariationsToFile();
            return newVariation;
        }

        public Variation CreateVariationForCompositionInstances(List<Instance> instances)
        {
            var changeSets = new Dictionary<Guid, Dictionary<Guid, InputValue>>();
            if (instances == null || instances.Count == 0)
            {
                Log.Warning("No instances to create variation for");
                return null;
            }

            Symbol parentSymbol = null;

            foreach (var instance in instances)
            {
                if (instance.Parent.Symbol.Id != SymbolId)
                {
                    Log.Error($"Instance {instance.SymbolChildId} is not a child of VariationPool operator {SymbolId}");
                    return null;
                }

                parentSymbol = instance.Parent.Symbol;

                var changeSet = new Dictionary<Guid, InputValue>();
                var hasAnimatableParameters = false;

                foreach (var input in instance.Inputs)
                {
                    if (!ValueUtils.BlendMethods.ContainsKey(input.Input.Value.ValueType))
                        continue;

                    hasAnimatableParameters = true;

                    if (input.Input.IsDefault)
                    {
                        continue;
                    }

                    if (ValueUtils.BlendMethods.ContainsKey(input.Input.Value.ValueType))
                    {
                        changeSet[input.Id] = input.Input.Value.Clone();
                    }
                }

                if (!hasAnimatableParameters)
                    continue;

                changeSets[instance.SymbolChildId] = changeSet;
            }

            var newVariation = new Variation
                                   {
                                       Id = Guid.NewGuid(),
                                       Title = "untitled",
                                       ActivationIndex = Variations.Count + 1, //TODO: First find the highest activation index
                                       IsPreset = false,
                                       PublishedDate = DateTime.Now,
                                       ParameterSetsForChildIds = changeSets,
                                   };

            var command = new AddPresetOrVariationCommand(parentSymbol, newVariation);
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

        private static MacroCommand CreateApplyVariationCommand(Instance compositionInstance, Variation variation)
        {
            var commands = new List<ICommand>();
            var compositionSymbol = compositionInstance.Symbol;

            foreach (var (childId, parameterSets) in variation.ParameterSetsForChildIds)
            {
                var symbolChild = compositionSymbol.Children.SingleOrDefault(s => s.Id == childId);
                if (symbolChild == null)
                {
                    //Log.Warning($"Ignoring childId {childId} in variation...");
                    continue;
                }

                if (childId == Guid.Empty)
                {
                    Log.Warning("Didn't expect parent-reference id in variation");
                    continue;
                }

                foreach (var input in symbolChild.Inputs.Values)
                {
                    if (!ValueUtils.BlendMethods.TryGetValue(input.Value.ValueType, out var blendFunction))
                        continue;

                    if (parameterSets.TryGetValue(input.InputDefinition.Id, out var param))
                    {
                        if (param == null)
                            continue;

                        var newCommand = new ChangeInputValueCommand(compositionSymbol, childId, input, param);
                        commands.Add(newCommand);
                    }
                    else
                    {
                        // Reset non-defaults
                        commands.Add(new ResetInputToDefault(compositionSymbol, childId, input));
                    }
                }
            }

            var command = new MacroCommand("Apply Variation Values", commands);
            return command;
        }

        private static MacroCommand CreateWeightedBlendSnapshotCommand(Instance compositionInstance, List<Variation> variations, IEnumerable<float> weights)
        {
            var commands = new List<ICommand>();
            var parentSymbol = compositionInstance.Symbol;
            var weightsArray = weights.ToArray();

            // Collect instances
            var affectedInstances = new HashSet<Guid>();
            foreach (var v in variations)
            {
                affectedInstances.UnionWith(v.ParameterSetsForChildIds.Keys);
            }

            foreach (var childId2 in affectedInstances)
            {
                var instance = compositionInstance.Children.SingleOrDefault(s => s.SymbolChildId == childId2);
                if (instance == null)
                    continue;

                // Collect variation parameters
                var variationParameterSets = new List<Dictionary<Guid, InputValue>>();
                foreach (var variation in variations)
                {
                    if (variation.ParameterSetsForChildIds.TryGetValue(childId2, out var parameterSet))
                    {
                        variationParameterSets.Add(parameterSet);
                    }
                }

                foreach (var inputSlot in instance.Inputs)
                {
                    if (!ValueUtils.WeightedBlendMethods.TryGetValue(inputSlot.Input.DefaultValue.ValueType, out var blendFunction))
                        continue;

                    var values = new List<InputValue>();
                    var definedForSome = false;

                    foreach (var parametersForInputs in variationParameterSets)
                    {
                        if (parametersForInputs.TryGetValue(inputSlot.Id, out var parameterValue))
                        {
                            if (parameterValue == null)
                                continue;

                            values.Add(parameterValue);
                            definedForSome = true;
                        }
                        else
                        {
                            values.Add(inputSlot.Input.DefaultValue);
                        }
                    }

                    if (definedForSome && weightsArray.Length == values.Count)
                    {
                        var mixed2 = blendFunction(values.ToArray(), weightsArray);
                        var newCommand = new ChangeInputValueCommand(parentSymbol, instance.SymbolChildId, inputSlot.Input, mixed2);
                        commands.Add(newCommand);
                    }
                }
            }

            var activeBlendCommand = new MacroCommand("Set Blended Snapshot Values", commands);
            return activeBlendCommand;
        }
        
        private static MacroCommand CreateBlendTowardsVariationCommand(Instance compositionInstance, Variation variation, float blend)
        {
            var commands = new List<ICommand>();
            if (!variation.IsSnapshot)
                return null;
            
            foreach (var child in compositionInstance.Children)
            {
                if (!variation.ParameterSetsForChildIds.TryGetValue(child.SymbolChildId, out var parametersForInputs))
                    continue;
                
                foreach (var inputSlot in child.Inputs)
                {
                    if (!ValueUtils.BlendMethods.TryGetValue(inputSlot.Input.DefaultValue.ValueType, out var blendFunction))
                        continue;

                    if (parametersForInputs.TryGetValue(inputSlot.Id, out var parameter))
                    {
                        if (parameter == null)
                            continue;

                        var mixed = blendFunction(inputSlot.Input.Value, parameter, blend);
                        var newCommand = new ChangeInputValueCommand(compositionInstance.Symbol, child.SymbolChildId, inputSlot.Input, mixed);
                        commands.Add(newCommand);
                    }
                    else if (!inputSlot.Input.IsDefault)
                    {
                        var mixed = blendFunction(inputSlot.Input.Value, inputSlot.Input.DefaultValue, blend);
                        var newCommand = new ChangeInputValueCommand(compositionInstance.Symbol, child.SymbolChildId, inputSlot.Input, mixed);
                        commands.Add(newCommand);
                    }
                }
            }

            var activeBlendCommand = new MacroCommand("Blend towards snapshot", commands);
            return activeBlendCommand;
        }
        
        
        private static MacroCommand CreateApplyPresetCommand(Instance instance, Variation variation)
        {
            var commands = new List<ICommand>();
            var parentSymbol = instance.Parent.Symbol;

            var symbolChild = parentSymbol.Children.SingleOrDefault(s => s.Id == instance.SymbolChildId);
            if (symbolChild != null)
            {
                foreach (var (childId, parametersForInputs) in variation.ParameterSetsForChildIds)
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

                            var newCommand = new ChangeInputValueCommand(parentSymbol, instance.SymbolChildId, inputSlot.Input, param);
                            commands.Add(newCommand);
                        }
                        else
                        {
                            // ResetOtherNonDefaults
                            commands.Add(new ResetInputToDefault(instance.Parent.Symbol, instance.SymbolChildId, inputSlot.Input));
                        }
                    }
                }
            }

            var command = new MacroCommand("Apply Preset Values", commands);
            return command;
        }

        private static MacroCommand CreateBlendToPresetCommand(Instance instance, Variation variation, float blend)
        {
            var commands = new List<ICommand>();
            var parentSymbol = instance.Parent.Symbol;

            var symbolChild = parentSymbol.Children.SingleOrDefault(s => s.Id == instance.SymbolChildId);
            if (symbolChild != null)
            {
                foreach (var (childId, parametersForInputs) in variation.ParameterSetsForChildIds)
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
                            var newCommand = new ChangeInputValueCommand(parentSymbol, instance.SymbolChildId, inputSlot.Input, mixed);
                            commands.Add(newCommand);
                        }
                        else if (!inputSlot.Input.IsDefault)
                        {
                            var mixed = blendFunction(inputSlot.Input.Value, inputSlot.Input.DefaultValue, blend);
                            var newCommand = new ChangeInputValueCommand(parentSymbol, instance.SymbolChildId, inputSlot.Input, mixed);
                            commands.Add(newCommand);
                        }
                    }
                }
            }

            var activeBlendCommand = new MacroCommand("Set Preset Values", commands);
            return activeBlendCommand;
        }

        private static MacroCommand CreateWeightedBlendPresetCommand(Instance instance, List<Variation> variations, IEnumerable<float> weights)
        {
            var commands = new List<ICommand>();
            var parentSymbol = instance.Parent.Symbol;
            var weightsArray = weights.ToArray();

            var symbolChild = parentSymbol.Children.SingleOrDefault(s => s.Id == instance.SymbolChildId);
            if (symbolChild != null)
            {
                // collect variation parameters
                var variationParameterSets = new List<Dictionary<Guid, InputValue>>();
                foreach (var variation in variations)
                {
                    foreach (var (childId, parametersForInputs) in variation.ParameterSetsForChildIds)
                    {
                        if (childId != Guid.Empty)
                        {
                            Log.Warning("Didn't expect childId in preset");
                            continue;
                        }

                        variationParameterSets.Add(parametersForInputs);
                    }
                }

                foreach (var inputSlot in instance.Inputs)
                {
                    if (!ValueUtils.WeightedBlendMethods.TryGetValue(inputSlot.Input.DefaultValue.ValueType, out var blendFunction))
                        continue;

                    var values = new List<InputValue>();
                    var definedForSome = false;

                    foreach (var parametersForInputs in variationParameterSets)
                    {
                        if (parametersForInputs.TryGetValue(inputSlot.Id, out var parameterValue))
                        {
                            if (parameterValue == null)
                                continue;

                            values.Add(parameterValue);
                            definedForSome = true;
                        }
                        else
                        {
                            values.Add(inputSlot.Input.DefaultValue);
                        }
                    }

                    if (definedForSome && weightsArray.Length == values.Count)
                    {
                        var mixed2 = blendFunction(values.ToArray(), weightsArray);
                        var newCommand = new ChangeInputValueCommand(parentSymbol, instance.SymbolChildId, inputSlot.Input, mixed2);
                        commands.Add(newCommand);
                    }
                }
            }

            var activeBlendCommand = new MacroCommand("Set Blended Preset Values", commands);
            return activeBlendCommand;
        }

        private static MatchTypes DoesPresetVariationMatch(Variation variation, Instance instance)
        {
            var setCorrectly = true;
            var foundOneMatch = false;
            var foundUnknownNonDefaults = false;

            foreach (var (symbolChildId, values) in variation.ParameterSetsForChildIds)
            {
                if (symbolChildId != Guid.Empty)
                    continue;

                foreach (var input in instance.Inputs)
                {
                    var inputIsDefault = input.Input.IsDefault;
                    var variationIncludesInput = values.ContainsKey(input.Id);

                    if (!ValueUtils.CompareFunctions.ContainsKey(input.ValueType))
                        continue;

                    if (variationIncludesInput)
                    {
                        foundOneMatch = true;

                        if (inputIsDefault)
                        {
                            setCorrectly = false;
                        }
                        else
                        {
                            var inputValueMatches = ValueUtils.CompareFunctions[input.ValueType](values[input.Id], input.Input.Value);
                            setCorrectly &= inputValueMatches;
                        }
                    }
                    else
                    {
                        if (inputIsDefault)
                        {
                        }
                        else
                        {
                            foundUnknownNonDefaults = true;
                        }
                    }
                }
            }

            if (!foundOneMatch || !setCorrectly)
            {
                return MatchTypes.NoMatch;
            }

            return foundUnknownNonDefaults ? MatchTypes.PresetParamsMatch : MatchTypes.PresetAndDefaultParamsMatch;
        }

        private enum MatchTypes
        {
            NoMatch,
            PresetParamsMatch,
            PresetAndDefaultParamsMatch,
        }

        private MacroCommand _activeBlendCommand;

        public static bool TryGetSnapshot(int activationIndex, out Variation variation)
        {
            variation = null;
            if (VariationHandling.ActivePoolForSnapshots == null)
                return false;

            foreach (var v in VariationHandling.ActivePoolForSnapshots.Variations)
            {
                if (v.ActivationIndex != activationIndex)
                    continue;

                variation = v;
                return true;
            }

            return false;
        }
    }
}