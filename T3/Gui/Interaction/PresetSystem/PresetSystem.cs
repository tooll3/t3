using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using NAudio.Midi;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Commands;
using T3.Gui.Graph;
using T3.Gui.Interaction.PresetSystem.Dialogs;
using T3.Gui.Interaction.PresetSystem.Midi;
using T3.Gui.Interaction.PresetSystem.Model;
using T3.Operators.Types.Id_59a0458e_2f3a_4856_96cd_32936f783cc5;

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

        private Guid _lastCompositionId;
        
        
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
            _nextCompositionUi = compositionUi;
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
            }

            if (ImGui.MenuItem("+ Add Group"))
            {
                SetOrCreateContextForActiveComposition();
                AddGroupDialog.ShowNextFrame();
            }

            ImGui.EndMenu();
        }
        #endregion

        //---------------------------------------------------------------------------------
        #region API calls from commands
        public void ActivateGroupAtIndex(int index)
        {
            if (ActiveContext == null)
                return;

            if (ActiveContext.Groups.Count <= index)
            {
                Log.Warning($"Tried activate group at {index}. There are only {ActiveContext.Groups.Count} defined.");
                return;
            }

            ActiveContext.ActiveGroupId = ActiveContext.Groups[index].Id;
        }

        public void SavePresetAtIndex(int buttonRangeIndex)
        {
            if (ActiveContext == null)
            {
                Log.Error($"Can't execute SavePresetAtIndex without valid context");
                return;
            }

            var address = ActiveContext.GetAddressFromButtonIndex(buttonRangeIndex);
            var group = ActiveContext.GetGroupForAddress(address);
            if (group == null)
            {
                Log.Warning($"Can't save preset for undefined group at {address}");
                return;
            }

            var scene = ActiveContext.GetSceneAt(address);
            if (scene == null)
            {
                ActiveContext.CreateSceneAt(address);
            }

            var newPreset = CreatePresetForGroup(group);
            ActiveContext.SetPresetAt(newPreset, address);
            group.SetActivePreset(newPreset);
            ActiveContext.WriteToJson();
        }

        public void ActivatePresetAtIndex(int buttonRangeIndex)
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
            group.SetActivePreset(preset);
            ActiveContext.SetGroupAsActive(group);

            //Log.Debug($"Applying preset at {address}");
            ApplyGroupPreset(group, preset);
            preset.State = Preset.States.Active;
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
        
        #endregion

        //---------------------------------------------------------------------------------
        #region InternalImplementation
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
            var instance = _activeCompositionInstance.Children.Single(c => c.SymbolChildId == newParameter.SymbolChildId);
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

        private PresetScene GetOrCreateActiveScene()
        {
            return null;
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
                var instance = _activeCompositionInstance.Children.Single(c => c.SymbolChildId == parameter.SymbolChildId);
                var input = instance.Inputs.Single(inp => inp.Id == parameter.InputId);
                newPreset.ValuesForGroupParameterIds[parameter.Id] = input.Input.Value.Clone();
            }

            return newPreset;
        }

        private void ApplyGroupPreset(ParameterGroup group, Preset preset)
        {
            var commands = new List<ICommand>();
            var symbol = _activeCompositionInstance.Symbol;
            //var symbolUi = SymbolUiRegistry.Entries[ActiveContext.CompositionId];

            foreach (var parameter in group.Parameters)
            {
                var symbolChild = symbol.Children.Single(s => s.Id == parameter.SymbolChildId);
                var input = symbolChild.InputValues[parameter.InputId];

                //var presetValuesForGroupParameterId = preset.ValuesForGroupParameterIds[parameter.Id];
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
        #endregion

        private Guid _activeCompositionId = Guid.Empty;
        private readonly List<IControllerInputDevice> _inputDevices;

        private readonly Dictionary<Guid, CompositionContext> _contextForCompositions =
            new Dictionary<Guid, CompositionContext>();

        //public Instance ActiveComposition;

        /// <summary>
        /// Is only changes by explicitly user actions:
        /// - switching to a composition with a preset context
        /// - creating a context (e.g. by added parameters to blending)
        /// - switching e.g. with the midi controllers 
        /// </summary>
        private CompositionContext ActiveContext { get; set; }

        private SymbolUi _nextCompositionUi;
        private SymbolChildUi _nextSymbolChildUi;
        private IInputSlot _nextInputSlotFor;
        private string _nextNameFor;

        private Instance _activeCompositionInstance;
        private static readonly AddGroupDialog AddGroupDialog = new AddGroupDialog();

        public void ActivateGroupAction(int obj)
        {
            throw new NotImplementedException();
        }
    }

    public interface IControllerInputDevice
    {
        void Update(PresetSystem presetSystem, MidiIn midiIn, CompositionContext context);
        int GetProductNameHash();
    }
}