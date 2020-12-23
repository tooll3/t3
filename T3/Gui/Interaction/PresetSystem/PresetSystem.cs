using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Commands;
using T3.Gui.Graph;
using T3.Gui.Interaction.PresetSystem.Dialogs;
using T3.Gui.Interaction.PresetSystem.Midi;
using T3.Gui.Interaction.PresetSystem.Model;
using T3.Gui.Selection;
using T3.Operators.Types.Id_59a0458e_2f3a_4856_96cd_32936f783cc5;
using T3.Operators.Types.Id_a53f3873_a5aa_4bcc_aa06_0745d98209d6;

namespace T3.Gui.Interaction.PresetSystem
{
    public class PresetSystem
    {
        public PresetSystem()
        {
            // Scan for output devices (e.g. to update LEDs etc.)
            MidiOutConnectionManager.Init();

            _inputDevices = new List<IControllerInputDevice>()
                                {
                                    new Apc40Mk2(this),
                                    new NanoControl8(this),
                                    new ApcMini(this),
                                };
        }

        //---------------------------------------------------------------------------------
        #region API from T3 UI
        public void Update()
        {
            // Sync with composition selected in UI
            var primaryGraphWindow = GraphWindow.GetVisibleInstances().FirstOrDefault();
            if (primaryGraphWindow == null)
                return;

            _activeCompositionInstance = primaryGraphWindow._graphCanvas.CompositionOp;
            _activeCompositionId = _activeCompositionInstance.Symbol.Id;
            _contextForCompositions.TryGetValue(_activeCompositionId, out var contextForCurrentComposition);

            // Attempt to read settings for composition
            if (contextForCurrentComposition == null)
            {
                if (_activeCompositionId != _lastCompositionId)
                {
                    var newContext = CompositionContext.ReadFromJson(_activeCompositionId);
                    if (newContext != null)
                    {
                        _contextForCompositions[_activeCompositionId] = newContext;
                        ActiveContext = newContext;
                    }
                    else
                    {
                        _lastCompositionId = _activeCompositionId;
                    }
                }
            }
            else
            {
                ActiveContext = contextForCurrentComposition;
            }

            // Update active context
            if (ActiveContext != null)
            {
                if (ActivatePresets.BlendSettingForCompositionIds.TryGetValue(ActiveContext.CompositionId, out var blendSetting))
                {
                    if (blendSetting.WasActivatedLastFrame)
                    {
                        Log.Debug("Blend setting was updated");
                        if (TryGetGroup(blendSetting.GroupIndex, out var group))
                        {
                            if (group != ActiveContext.ActiveGroup)
                            {
                                ActivateGroupAtIndex(blendSetting.GroupIndex);
                            }

                            //ActivateOrCreatePresetAtIndex(blendSetting.PresetAIndex);
                            TryActivatePresetAtAddress(new PresetAddress(blendSetting.GroupIndex, blendSetting.PresetAIndex));
                            blendSetting.WasActivatedLastFrame = false;
                        }
                    }
                }
            }

            // Update Midi Devices 
            foreach (var connectedDevice in _inputDevices)
            {
                // TODO: support generic input controllers with arbitrary DeviceId 
                var midiIn = MidiInConnectionManager.GetMidiInForProductNameHash(connectedDevice.GetProductNameHash());
                if (midiIn == null)
                    continue;

                connectedDevice.Update(this, midiIn, ActiveContext);
            }

            // Draw Ui
            AddGroupDialog.Draw(ref _nextNameFor);

            UpdateInputReferences();
        }

        public void CreateNewGroupForInput()
        {
            // if (!(_nextInputSlotFor.Input.Value is InputValue<float> v))
            // {
            //     Log.Warning("Sorry, but for now only float parameters can be blended. Is " + _nextInputSlotFor.MappedType);
            //     return;
            // }

            SetOrCreateContextForActiveComposition();
            var group = ActiveContext.AppendNewGroup(_nextNameFor);
            group.AddParameterToIndex(CreateParameter(), 0);
            ActiveContext.ActiveGroupId = group.Id;
        }

        public void DrawInputContextMenu(IInputSlot inputSlot, SymbolUi compositionUi, SymbolChildUi symbolChildUi)
        {
            // Save relevant creation details
            //_nextCompositionUi = compositionUi;
            _nextSymbolChildUi = symbolChildUi;
            _nextInputSlotFor = inputSlot;
            _nextNameFor = symbolChildUi.SymbolChild.ReadableName;

            CustomComponents.HintLabel("Group");
            if (ActiveContext != null)
            {
                foreach (var group in ActiveContext.Groups)
                {
                    ImGui.PushID(group.Id.GetHashCode());

                    if (ImGui.BeginMenu(group.Title))
                    {
                        CustomComponents.HintLabel("Parameter");

                        for (var parameterIndex = 0; parameterIndex < 8; parameterIndex++)
                        {
                            ImGui.PushID(parameterIndex);

                            var slotId = $"{parameterIndex + 1}. ";
                            var hasParameter = parameterIndex < group.Parameters.Count() && group.Parameters[parameterIndex] != null;
                            var wasSelected = hasParameter
                                                  ? ImGui.MenuItem(slotId + group.Parameters[parameterIndex].Title)
                                                  : ImGui.MenuItem(slotId + "+");

                            if (wasSelected)
                            {
                                ActiveContext.SetGroupAsActive(group);
                                CreateNewParameterForActiveGroup(parameterIndex);
                            }

                            ImGui.PopID();
                        }

                        ImGui.EndMenu();
                    }

                    ImGui.PopID();
                }

                var activeGroup = ActiveContext?.ActiveGroup;
                if (activeGroup != null)
                {
                    if (ImGui.MenuItem("Append to G" + activeGroup.Index))
                    {
                        var index = activeGroup.FindNextFreeIndex();
                        CreateNewParameterForActiveGroup(index);
                    }
                }
            }

            if (ImGui.MenuItem("+ Add Group"))
            {
                SetOrCreateContextForActiveComposition();
                AddGroupDialog.ShowNextFrame();
            }

            ImGui.EndMenu();
        }

        public ParameterGroup GetBlendGroupForHashedInput(int symbolChildInputHash)
        {
            _groupForBlendedParameters.TryGetValue(symbolChildInputHash, out var result);
            return result;
        }

        public void RemoveBlending(int symbolChildInputHash)
        {
            _groupForBlendedParameters.TryGetValue(symbolChildInputHash, out var parameterGroup);
            if (parameterGroup == null)
                return;

            for (var parameterIndex = 0; parameterIndex < parameterGroup.Parameters.Count; parameterIndex++)
            {
                var p = parameterGroup.Parameters[parameterIndex];
                if (p.GetHashForInput() != symbolChildInputHash)
                    continue;

                parameterGroup.Parameters[parameterIndex] = null;
            }
        }

        public void ActivateGroupAtIndex(int index)
        {
            if (!TryGetGroup(index, out var group))
                return;

            var isGroupTriggeredAgain = ActiveContext.ActiveGroup == group;
            if (isGroupTriggeredAgain)
            {
                ActiveContext.IsGroupExpanded = !ActiveContext.IsGroupExpanded;
            }
            else
            {
                ActiveContext.ActiveGroupId = @group.Id;
                SelectUiElementsForGroup(group);
            }

            HighlightIdenticalPresets();
        }

        public void SavePresetAtIndex(int buttonRangeIndex)
        {
            if (ActiveContext == null)
            {
                Log.Error($"Can't execute SavePresetAtIndex without valid context");
                return;
            }

            var address = ActiveContext.GetAddressFromButtonIndex(buttonRangeIndex);
            CreatePresetAtAddress(address);
        }
        #endregion

        //---------------------------------------------------------------------------------
        #region API calls from midi inputs
        public void ActivateOrCreatePresetAtIndex(int buttonRangeIndex)
        {
            if (ActiveContext == null)
            {
                Log.Error($"Can't execute ApplyPresetAtIndex without valid context");
                return;
            }

            var address = ActiveContext.GetAddressFromButtonIndex(buttonRangeIndex);
            if (!TryGetPreset(address, out var preset))
            {
                Log.Info($"There is no preset at {address}. Creating one.");
                CreatePresetAtAddress(address);
                return;
            }

            Log.Info($"Activating preset at {address}...");

            var group = ActiveContext.GetGroupForAddress(address);
            ActivatePreset(group, preset);
            HighlightIdenticalPresets();
        }

        private void TryActivatePresetAtAddress(PresetAddress address)
        {
            if (TryGetGroup(address.GroupColumn, out var group)
                && TryGetPreset(address, out var preset))
            {
                ActivatePreset(@group, preset);
            }
        }

        private void ActivatePreset(ParameterGroup @group, Preset preset)
        {
            @group.SetActivePreset(preset);
            ActiveContext.SetGroupAsActive(@group);

            ApplyGroupPreset(@group, preset);
            preset.State = Preset.States.Active;
            HighlightIdenticalPresets();
        }

        public void RemovePresetAtIndex(int buttonRangeIndex)
        {
            if (ActiveContext == null)
            {
                Log.Error($"Can't execute ApplyPresetAtIndex without valid context");
                return;
            }

            var address = ActiveContext.GetAddressFromButtonIndex(buttonRangeIndex);
            var preset = ActiveContext.TryGetPresetAt(address);
            if (preset == null)
            {
                Log.Info($"There is no preset at {address}");
                return;
            }

            var group = ActiveContext.GetGroupForAddress(address);
            group.SetActivePreset(null);
            ActiveContext.Presets[address.GroupColumn, address.SceneRow] = null;
            ApplyGroupPreset(group, preset);
            preset.State = Preset.States.Active;
            ActiveContext.WriteToJson();
        }

        public void StartBlendingPresets(int[] indices)
        {
            Log.Debug(" Start blending " + String.Join(", ", indices));
            if (ActiveContext == null)
                return;

            for (var groupIndex = 0; groupIndex < ActiveContext.Groups.Count; groupIndex++)
            {
                var @group = ActiveContext.Groups[groupIndex];
                if (@group == null)
                    continue;

                var startedNewBlendGroup = false;
                foreach (var index in indices)
                {
                    var address = ActiveContext.GetAddressFromButtonIndex(index);
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

        public void BlendValuesUpdate(int groupIndex, float value)
        {
            if (!TryGetGroup(groupIndex, out var @group))
                return;

            BlendGroupPresets(group, value / 127f);
        }

        public void AppendPresetToCurrentGroup()
        {
            if (ActiveContext == null)
                return;

            var group = ActiveContext.ActiveGroup;
            if (group == null)
                return;

            var sceneRowsCount = ActiveContext.Presets.GetLength(1);
            var address = new PresetAddress(group.Index, 0);
            int minFreeIndex = sceneRowsCount;
            int sceneRowIndex = sceneRowsCount - 1;
            for (; sceneRowIndex >= 0; sceneRowIndex--)
            {
                address.SceneRow = sceneRowIndex;
                var preset = ActiveContext.TryGetPresetAt(address);
                if (preset == null)
                {
                    minFreeIndex = sceneRowIndex;
                }
            }

            address.SceneRow = minFreeIndex;
            CreatePresetAtAddress(address);
        }
        #endregion

        //---------------------------------------------------------------------------------
        #region InternalImplementation
        /// <summary>
        /// Tries to get get a group by index. Verifies Context, indeces, etc.  
        /// </summary>
        private bool TryGetGroup(int groupIndex, out ParameterGroup @group)
        {
            group = null;
            if (ActiveContext == null || ActiveContext.Groups == null)
            {
                Log.Warning("Active context is undefined");
                return false;
            }

            if (groupIndex < 0 || groupIndex >= ActiveContext.Groups.Count)
            {
                Log.Warning("Can't blend undefined group index " + groupIndex);
                return false;
            }

            @group = ActiveContext.Groups[groupIndex];
            if (@group == null)
            {
                Log.Warning($"can't find group with index {groupIndex}");
                return false;
            }

            return true;
        }

        private bool TryGetPreset(PresetAddress address, out Preset preset)
        {
            preset = null;

            var activeContextPresets = ActiveContext?.Presets;
            if (activeContextPresets == null)
                return false;

            if (address.GroupColumn < 0 || address.GroupColumn >= activeContextPresets.GetLength(0)
                                        || address.SceneRow < 0 || address.SceneRow >= activeContextPresets.GetLength(1))
                return false;

            preset = activeContextPresets[address.GroupColumn, address.SceneRow];
            return preset != null;
        }

        private void HighlightIdenticalPresets()
        {
            var activePreset = ActiveContext?.ActiveGroup?.ActivePreset;
            //activePreset?.UpdateStateIfCurrentOrModified(ActiveContext?.ActiveGroup, _activeCompositionInstance);

            var activeGroup = ActiveContext?.ActiveGroup;
            if (activeGroup == null || _activeCompositionInstance == null)
                return;

            foreach (var preset in ActiveContext.GetPresetsForGroup(activeGroup))
            {
                if (preset == null)
                    continue;

                var isActive = preset == activePreset;
                preset.UpdateStateIfCurrentOrModified(activeGroup, _activeCompositionInstance, isActive);
            }
        }

        private void UpdateInputReferences()
        {
            _groupForBlendedParameters.Clear();
            if (ActiveContext == null)
                return;

            for (var groupIndex = 0; groupIndex < ActiveContext.Groups.Count; groupIndex++)
            {
                var group = ActiveContext.Groups[groupIndex];
                group.Index = groupIndex;

                foreach (var parameter in @group.Parameters)
                {
                    if (parameter == null)
                        continue;

                    _groupForBlendedParameters[parameter.GetHashForInput()] = @group;
                }
            }
        }

        private void SelectUiElementsForGroup(ParameterGroup group)
        {
            if (ActiveContext == null)
                return;

            SelectionManager.Clear();

            if (!SymbolUiRegistry.Entries.TryGetValue(_activeCompositionId, out var symbolUi))
                return;

            foreach (var parameter in @group.Parameters)
            {
                if (parameter == null)
                    continue;

                var symbolChildUi = symbolUi.ChildUis.SingleOrDefault(childUi => childUi.Id == parameter.SymbolChildId);
                var instance = _activeCompositionInstance.Children.SingleOrDefault(child => child.SymbolChildId == parameter.SymbolChildId);
                if (symbolChildUi != null && instance != null)
                {
                    SelectionManager.AddSelection(symbolChildUi, instance);
                }
            }

            SelectionManager.FitViewToSelection();
        }

        private readonly Dictionary<int, ParameterGroup> _groupForBlendedParameters = new Dictionary<int, ParameterGroup>(100);

        private void CreatePresetAtAddress(PresetAddress address)
        {
            var group = ActiveContext.GetGroupForAddress(address);
            if (@group == null)
            {
                Log.Warning($"Can't save preset for undefined group at {address}");
                return;
            }

            var scene = ActiveContext.GetSceneAt(address);
            if (scene == null)
            {
                ActiveContext.CreateSceneAt(address);
            }

            var newPreset = CreatePresetForGroup(@group);
            ActiveContext.SetPresetAt(newPreset, address);
            @group.SetActivePreset(newPreset);
            ActiveContext.WriteToJson();
        }

        private void CreateNewParameterForActiveGroup(int parameterIndex)
        {
            SetOrCreateContextForActiveComposition();
            var activeGroup = ActiveContext.ActiveGroup;
            if (activeGroup == null)
            {
                Log.Warning("Can't save parameter without active group");
                return;
            }

            var newParameter = activeGroup.AddParameterToIndex(CreateParameter(), parameterIndex);
            var instance = _activeCompositionInstance.Children.SingleOrDefault(c => c.SymbolChildId == newParameter.SymbolChildId);
            if (instance == null)
            {
                Log.Warning("Can't find correct instance of parameter view");
                return;
            }

            var input = instance.Inputs.Single(inp => inp.Id == newParameter.InputId);
            foreach (var preset in ActiveContext.GetPresetsForGroup(activeGroup))
            {
                preset.ValuesForGroupParameterIds[newParameter.Id] = input.Input.Value.Clone();
            }
        }

        private GroupParameter CreateParameter()
        {
            var newParameter = new GroupParameter
                                   {
                                       Id = Guid.NewGuid(),
                                       SymbolChildId = _nextSymbolChildUi.Id,
                                       InputId = _nextInputSlotFor.Id,
                                       // ComponentIndex = 0,
                                       // InputType = _nextInputSlotFor.ValueType,
                                       Title = _nextSymbolChildUi.SymbolChild.ReadableName + "." + _nextInputSlotFor.Input.Name,
                                   };
            return newParameter;
        }

        private void SetOrCreateContextForActiveComposition()
        {
            if (_contextForCompositions.TryGetValue(_activeCompositionId, out var existingContext))
            {
                ActiveContext = existingContext;
                return;
            }

            ActiveContext = new CompositionContext()
                                {
                                    CompositionId = _activeCompositionId,
                                };
            _contextForCompositions[_activeCompositionId] = ActiveContext;
        }

        private Preset CreatePresetForGroup(ParameterGroup group)
        {
            if (ActiveContext.CompositionId != _activeCompositionInstance.Symbol.Id)
            {
                Log.Error("Can't create preset because composition instance does not match");
                return null;
            }

            var newPreset = new Preset();
            //var operatorSymbol = SymbolRegistry.Entries[_activeCompositionId];
            foreach (var parameter in group.Parameters)
            {
                //var symbolChild = operatorSymbol.Children.Single(child => child.Id == parameter.SymbolChildId);
                var instance = _activeCompositionInstance.Children.SingleOrDefault(c => c.SymbolChildId == parameter.SymbolChildId);
                if (instance == null)
                {
                    Log.Error("Failed to get instance to focus parameters " + parameter.Title);
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
            var symbol = _activeCompositionInstance.Symbol;

            foreach (var parameter in group.Parameters)
            {
                var symbolChild = symbol.Children.SingleOrDefault(s => s.Id == parameter.SymbolChildId);
                if (symbolChild == null)
                {
                    Log.Error("Can't find symbol child");
                    return;
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

        private void BlendGroupPresets(ParameterGroup group, float blendValue)
        {
            var commands = new List<ICommand>();
            var symbol = _activeCompositionInstance.Symbol;

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

            foreach (var parameter in group.Parameters)
            {
                var symbolChild = symbol.Children.Single(s => s.Id == parameter.SymbolChildId);
                var input = symbolChild.InputValues[parameter.InputId];

                if (!group.BlendedPresets[index0].ValuesForGroupParameterIds.TryGetValue(parameter.Id, out var valueA)
                    || !group.BlendedPresets[index1].ValuesForGroupParameterIds.TryGetValue(parameter.Id, out var valueB))
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

        private Guid _activeCompositionId = Guid.Empty;
        private readonly List<IControllerInputDevice> _inputDevices;

        private readonly Dictionary<Guid, CompositionContext> _contextForCompositions = new Dictionary<Guid, CompositionContext>();

        /// <summary>
        /// Is only changes by explicitly user actions:
        /// - switching to a composition with a preset context
        /// - creating a context (e.g. by added parameters to blending)
        /// - switching e.g. with the midi controllers 
        /// </summary>
        private CompositionContext ActiveContext { get; set; }

        private Guid _lastCompositionId;

        private SymbolChildUi _nextSymbolChildUi;
        private IInputSlot _nextInputSlotFor;
        private string _nextNameFor;

        private Instance _activeCompositionInstance;
        private static readonly AddGroupDialog AddGroupDialog = new AddGroupDialog();
    }
}