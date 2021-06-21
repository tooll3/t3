using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;

namespace T3.Gui.Interaction.Variation.Model
{
    /// <summary>
    /// Implements operators on to preset groups
    /// </summary>
    public partial class OperatorVariation
    {
        internal ParameterGroup GetBlendGroupForHashedInput(int symbolChildInputHash)
        {
            _groupForBlendedParameters.TryGetValue(symbolChildInputHash, out var result);
            return result;
        }

        internal void RemoveBlending(int symbolChildInputHash)
        {
            _groupForBlendedParameters.TryGetValue(symbolChildInputHash, out var parameterGroup);
            if (parameterGroup == null)
                return;

            for (var parameterIndex = 0; parameterIndex < parameterGroup.Parameters.Count; parameterIndex++)
            {
                var p = parameterGroup.Parameters[parameterIndex];
                if (p == null || p.GetHashForInput() != symbolChildInputHash)
                    continue;

                parameterGroup.Parameters[parameterIndex] = null;
            }

            PurgeNullParametersInGroup(parameterGroup);
            UpdateInputReferences();
        }

        /// <summary>
        /// Toggles group expansion if active group activated.
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns>True if selection required</returns>
        internal bool ActivateGroupAtIndex(int index)
        {
            if (!TryGetGroup(index, out var group))
                return false;

            var isGroupTriggeredAgain = ActiveGroup == group;
            if (isGroupTriggeredAgain)
            {
                IsGroupExpanded = !IsGroupExpanded;
            }
            else
            {
                ActiveGroupId = group.Id;
                return true;
            }

            HighlightIdenticalPresets(group);
            return false;
        }

        internal void SavePresetAtIndex(int buttonRangeIndex)
        {

            var address = GetAddressFromButtonIndex(buttonRangeIndex);
            CreatePresetAtAddress(address);
        }


        internal void ActivateOrCreatePresetAtIndex(int buttonRangeIndex)
        {
            var address = GetAddressFromButtonIndex(buttonRangeIndex);
            if (!TryGetPreset(address, out var preset))
            {
                Log.Info($"There is no preset at {address}. Creating one.");
                CreatePresetAtAddress(address);
                return;
            }

            Log.Info($"Activating preset at {address}...");

            var group = GetGroupForAddress(address);
            ActivatePreset(group, preset);
            HighlightIdenticalPresets(group);
        }

        internal void TryActivatePresetAtAddress(PresetAddress address)
        {
            if (TryGetGroup(address.GroupColumn, out var group)
                && TryGetPreset(address, out var preset))
            {
                ActivatePreset(group, preset);
            }
        }

        internal void ActivatePreset(ParameterGroup group, Preset preset)
        {
            group.SetActivePreset(preset);
            SetGroupAsActive(group);

            if (group.BlendTransitionDuration > 0)
            {
                StartBlendTransitionIntoPreset(group, preset);
            }
            else
            {
                ApplyGroupPreset(group, preset);
            }
            
            preset.State = Preset.States.Active;
            HighlightIdenticalPresets(group);
        }

        internal void RemovePresetAtIndex(int buttonRangeIndex)
        {
            var address = GetAddressFromButtonIndex(buttonRangeIndex);
            RemovePresetAtAddress(address);
        }

        internal void RemovePresetAtAddress(PresetAddress address)
        {
            var preset = TryGetPresetAt(address);
            if (preset == null)
            {
                Log.Info($"There is no preset at {address}");
                return;
            }

            var group = GetGroupForAddress(address);
            group.SetActivePreset(null);
            Presets[address.GroupColumn, address.SceneRow] = null;
            ApplyGroupPreset(group, preset);
            preset.State = Preset.States.Active;
            WriteToJson();
        }

        internal void StartBlendingPresets(int[] indices)
        {
            for (var groupIndex = 0; groupIndex < Groups.Count; groupIndex++)
            {
                var group = Groups[groupIndex];
                if (group == null)
                    continue;

                var startedNewBlendGroup = false;
                foreach (var index in indices)
                {
                    var address = GetAddressFromButtonIndex(index);
                    if (address.GroupColumn != groupIndex)
                        continue;

                    if (!startedNewBlendGroup)
                    {
                        group.StopBlending();
                        startedNewBlendGroup = true;
                    }

                    if (!TryGetPreset(address, out var preset))
                        return;

                    preset.State = Preset.States.IsBlended;
                    group.BlendedPresets.Add(preset);
                }
            }
        }

        internal void BlendValuesUpdate(int groupIndex, float value)
        {
            if (!TryGetGroup(groupIndex, out var group))
                return;

            BlendGroupPresets(group, value / 127f);
        }

        internal void AppendPresetToCurrentGroup()
        {
            var group = ActiveGroup;
            if (group == null)
                return;

            var sceneRowsCount = Presets.GetLength(1);
            var address = new PresetAddress(group.Index, 0);
            int minFreeIndex = sceneRowsCount;
            int sceneRowIndex = sceneRowsCount - 1;
            for (; sceneRowIndex >= 0; sceneRowIndex--)
            {
                address.SceneRow = sceneRowIndex;
                var preset = TryGetPresetAt(address);
                if (preset == null)
                {
                    minFreeIndex = sceneRowIndex;
                }
            }

            address.SceneRow = minFreeIndex;
            CreatePresetAtAddress(address);
        }

        //---------------------------------------------------------------------------------
        #region InternalImplementation
        /// <summary>
        /// Tries to get get a group by index. Verifies indices, etc.  
        /// </summary>
        internal bool TryGetGroup(int groupIndex, out ParameterGroup group)
        {
            group = null;
            if (Groups == null)
            {
                Log.Warning("groups for variations is undefined");
                return false;
            }

            if (groupIndex < 0 || groupIndex >= Groups.Count)
            {
                Log.Warning("Can't blend undefined group index " + groupIndex);
                return false;
            }

            group = Groups[groupIndex];
            if (group == null)
            {
                Log.Warning($"can't find group with index {groupIndex}");
                return false;
            }

            return true;
        }

        private bool TryGetPreset(PresetAddress address, out Preset preset)
        {
            preset = null;

            var activeVariationsPresets = Presets;
            if (activeVariationsPresets == null)
                return false;

            if (address.GroupColumn < 0 || address.GroupColumn >= activeVariationsPresets.GetLength(0)
                                        || address.SceneRow < 0 || address.SceneRow >= activeVariationsPresets.GetLength(1))
                return false;

            preset = activeVariationsPresets[address.GroupColumn, address.SceneRow];
            return preset != null;
        }

        private void HighlightIdenticalPresets(ParameterGroup group)
        {
            //activePreset?.UpdateStateIfCurrentOrModified(ActiveGroup, _activeCompositionInstance);

            //var activeGroup = ActiveGroup;
            if (group == null || CompositionInstance == null)
                return;
            
            var activePreset = group.ActivePreset;

            foreach (var preset in GetPresetsForGroup(group))
            {
                if (preset == null)
                    continue;

                var isActive = preset == activePreset;
                preset.UpdateStateIfCurrentOrModified(group, CompositionInstance, isActive);
            }
        }

        /// <summary>
        /// An ugly hack to allow highlighting of parameters assigned to PresetParameter groups
        /// </summary>
        internal void UpdateInputReferences()
        {
            _groupForBlendedParameters.Clear();
            
            for (var groupIndex = 0; groupIndex < Groups.Count; groupIndex++)
            {
                var group = Groups[groupIndex];
                group.Index = groupIndex;

                foreach (var parameter in group.Parameters)
                {
                    if (parameter == null)
                        continue;

                    _groupForBlendedParameters[parameter.GetHashForInput()] = group;
                }
            }
        }



        internal void CreatePresetAtAddress(PresetAddress address)
        {
            var group = GetGroupForAddress(address);
            if (group == null)
            {
                Log.Warning($"Can't save preset for undefined group at {address}");
                return;
            }

            var scene = GetSceneAt(address);
            if (scene == null)
            {
                CreateSceneAt(address);
            }

            var newPreset = CreatePresetForGroup(group);
            SetPresetAt(newPreset, address);
            group.SetActivePreset(newPreset);
            WriteToJson();
        }

        internal void StartBlendTransitionIntoPreset(ParameterGroup group, Preset targetPreset)
        {
            group.BlendStartPreset = CreatePresetForGroup(group);
            group.BlendTargetPreset = targetPreset;
            //_transitionStartTime = EvaluationContext.BeatTime;
            group.BlendTransitionProgress = 0;
        }

        internal void UpdateBlendTransition(ParameterGroup group)
        {
            if (@group?.BlendStartPreset == null)
                return;
            
            group.BlendTransitionProgress += (float)EvaluationContext.LastFrameDuration / group.BlendTransitionDuration;
            if (group.BlendTransitionProgress >= 1)
            {
                ApplyGroupPreset(group, group.BlendTargetPreset);
                group.BlendStartPreset = null;
                group.BlendTargetPreset = null;
                return;
            }
            
            BlendTwoPresets(group, group.BlendStartPreset, group.BlendTargetPreset, group.BlendTransitionProgress);
        }
        
        private Preset CreatePresetForGroup(ParameterGroup group)
        {
            if (CompositionId != CompositionInstance.Symbol.Id)
            {
                Log.Error("Can't create preset because composition instance does not match");
                return null;
            }

            var newPreset = new Preset();
            foreach (var parameter in group.Parameters)
            {
                if (parameter == null)
                    continue;

                var instance = CompositionInstance.Children.SingleOrDefault(c => c.SymbolChildId == parameter.SymbolChildId);
                if (instance == null)
                {
                    continue;
                }

                var input = instance.Inputs.Single(inp => inp.Id == parameter.InputId);
                newPreset.ValuesForGroupParameterIds[parameter.Id] = input.Input.Value.Clone();
            }

            return newPreset;
        }

        private void ApplyGroupPreset(ParameterGroup group, Preset preset)
        {
            var commands = new List<ICommand>();
            var symbol = CompositionInstance.Symbol;

            foreach (var parameter in group.Parameters)
            {
                if (parameter == null)
                    continue;

                var symbolChild = symbol.Children.SingleOrDefault(s => s.Id == parameter.SymbolChildId);
                if (symbolChild == null)
                {
                    //Log.Error("Can't find symbol child");
                    continue;
                }

                var input = symbolChild.InputValues[parameter.InputId];

                if (preset.ValuesForGroupParameterIds.TryGetValue(parameter.Id, out var presetValuesForGroupParameterId))
                {
                    var newCommand = new ChangeInputValueCommand(symbol, parameter.SymbolChildId, input)
                                         {
                                             Value = presetValuesForGroupParameterId,
                                         };
                    commands.Add(newCommand);
                }
                else
                {
                    Log.Warning($"Preset doesn't contain value for parameter {parameter.Title}");
                }
            }

            var command = new MacroCommand("Set Preset Values", commands);
            UndoRedoStack.AddAndExecute(command);
        }

        internal void BlendGroupPresets(ParameterGroup group, float blendValue)
        {
            if (group.BlendedPresets.Count < 2)
            {
                Log.Warning($"Select at least two presets for blending ({group.BlendedPresets.Count} selected)");
                return;
            }

            var count = group.BlendedPresets.Count;
            var clampedBlend = blendValue.Clamp(0, 1);
            var t = clampedBlend * (count - 1);
            var index0 = (int)t.Clamp(0, count - 2);
            var index1 = index0 + 1;
            var localBlendFactor = t - index0;

            var groupBlendedPresetA = group.BlendedPresets[index0];
            var groupBlendedPresetB = group.BlendedPresets[index1];
            
            BlendTwoPresets(group, groupBlendedPresetA, groupBlendedPresetB, localBlendFactor);
        }

        private void BlendTwoPresets(ParameterGroup group, Preset groupBlendedPresetA, Preset groupBlendedPresetB, float localBlendFactor)
        {
            var commands = new List<ICommand>();
            var symbol = CompositionInstance.Symbol;
            foreach (var parameter in group.Parameters)
            {
                var symbolChild = symbol.Children.SingleOrDefault(s => s.Id == parameter.SymbolChildId);
                if (symbolChild == null)
                    continue;

                var input = symbolChild.InputValues[parameter.InputId];

                if (!groupBlendedPresetA.ValuesForGroupParameterIds.TryGetValue(parameter.Id, out var valueA)
                    || !groupBlendedPresetB.ValuesForGroupParameterIds.TryGetValue(parameter.Id, out var valueB))
                    continue;

                if (valueA is InputValue<float> floatValueA && valueB is InputValue<float> floatValueB)
                {
                    var blendedValue = MathUtils.Lerp(floatValueA.Value, floatValueB.Value, localBlendFactor);
                    commands.Add(new ChangeInputValueCommand(symbol, parameter.SymbolChildId, input)
                                     {
                                         Value = new InputValue<float>(blendedValue),
                                     });
                }
                else if (valueA is InputValue<Vector2> vec2ValueA && valueB is InputValue<Vector2> vec2ValueB)
                {
                    var blendedValue = MathUtils.Lerp(vec2ValueA.Value, vec2ValueB.Value, localBlendFactor);
                    commands.Add(new ChangeInputValueCommand(symbol, parameter.SymbolChildId, input)
                                     {
                                         Value = new InputValue<Vector2>(blendedValue),
                                     });
                }
                else if (valueA is InputValue<Vector3> vec3ValueA && valueB is InputValue<Vector3> vec3ValueB)
                {
                    var blendedValue = MathUtils.Lerp(vec3ValueA.Value, vec3ValueB.Value, localBlendFactor);
                    commands.Add(new ChangeInputValueCommand(symbol, parameter.SymbolChildId, input)
                                     {
                                         Value = new InputValue<Vector3>(blendedValue),
                                     });
                }
                else if (valueA is InputValue<Vector4> vec4ValueA && valueB is InputValue<Vector4> vec4ValueB)
                {
                    var blendedValue = MathUtils.Lerp(vec4ValueA.Value, vec4ValueB.Value, localBlendFactor);
                    commands.Add(new ChangeInputValueCommand(symbol, parameter.SymbolChildId, input)
                                     {
                                         Value = new InputValue<Vector4>(blendedValue),
                                     });
                }
            }

            var command = new MacroCommand("Set Preset Values", commands);
            command.Do(); // No Undo... boo! 
        }
        #endregion
        
        private readonly Dictionary<int, ParameterGroup> _groupForBlendedParameters = new Dictionary<int, ParameterGroup>(100);
        
    }
}