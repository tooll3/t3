using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using NAudio.Midi;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Graph;
using T3.Gui.Interaction.PresetSystem.Dialogs;
using T3.Gui.Interaction.PresetSystem.Midi;
using T3.Operators.Types.Id_59a0458e_2f3a_4856_96cd_32936f783cc5;

namespace T3.Gui.Interaction.PresetSystem
{
    public class PresetSystem
    {
        public PresetSystem()
        {
            // Scan for output devices (e.g. to update LEDs etc.)
            MidiOutConnectionManager.Init();

            // Get input devices
            _inputDevices = new List<IControllerInputDevice>()
                                {
                                    new NanoControl8(),
                                    new ApcMiniDevice(),
                                };

            // Adding dummy configuration
            _presetConfigurationForCompositions[Guid.Empty] = new PresetConfiguration();
        }

        public void Update()
        {
            var primaryGraphWindow = GraphWindow.GetVisibleInstances().FirstOrDefault();
            if (primaryGraphWindow == null)
                return;

            _activeCompositionId = primaryGraphWindow._graphCanvas.CompositionOp.Symbol.Id;
            var activeConfig = ConfigForActiveComposition;
            if (activeConfig == null)
                return;

            foreach (var inputDevice in _inputDevices)
            {
                // TODO: support generic input controllers with arbitrary DeviceId 
                var midiIn = MidiInConnectionManager.GetMidiInForProductNameHash(inputDevice.GetProductNameHash());
                if (midiIn == null)
                    continue;

                inputDevice.Update(this, midiIn, ConfigForActiveComposition);
            }

            AddGroupDialog.Draw(ref _nextNameFor);
        }

        public void CreateNewGroupForInput()
        {
            if (!(_nextInputSlotFor.Input.Value is InputValue<float> v))
            {
                Log.Warning("Sorry, but for now only float parameters can be blended. Is " + _nextInputSlotFor.MappedType);
                return;
            }

            var activeConfiguration = GetOrCreateActiveConfiguration();
            var group = activeConfiguration.CreateNewGroup(_nextNameFor);
            group.AddParameterToIndex(CreateParameter(), 0);
            activeConfiguration.ActiveGroupId = group.Id;
        }

        private GroupParameter CreateParameter()
        {
            var newParameter = new GroupParameter
                                   {
                                       Id = new Guid(),
                                       SymbolChildId = _nextSymbolChildUi.Id,
                                       InputId = _nextInputSlotFor.Id,
                                       ComponentIndex = 0,
                                       InputType = _nextInputSlotFor.ValueType,
                                       Title = _nextSymbolChildUi.SymbolChild.ReadableName + "." + _nextInputSlotFor.Input.Name,
                                   };
            return newParameter;
        }

        public void CreateNewParameterForActiveGroup(int index)
        {
            var activeConfiguration = GetOrCreateActiveConfiguration();
            var activeGroup = activeConfiguration.ActiveGroup;
            if (activeGroup == null)
            {
                Log.Warning("Can't save parameter without active group");
                return;
            }

            activeGroup.AddParameterToIndex(CreateParameter(), index);
        }

        private void InsertNewParameter(ParameterGroup @group, int index)
        {
        }

        private PresetConfiguration GetOrCreateActiveConfiguration()
        {
            if (!_presetConfigurationForCompositions.TryGetValue(_activeCompositionId, out var activeConfiguration))
            {
                activeConfiguration = new PresetConfiguration();
                _presetConfigurationForCompositions[_activeCompositionId] = activeConfiguration;
            }

            return activeConfiguration;
        }

        public void InitializeForComposition(Guid symbolId)
        {
            _presetConfigurationForCompositions[symbolId] = new PresetConfiguration(); // TODO: this should be deserialized
        }

        public PresetConfiguration ConfigForActiveComposition
        {
            get
            {
                _presetConfigurationForCompositions.TryGetValue(_activeCompositionId, out var config);
                return config;
            }
        }

        private Guid _activeCompositionId = Guid.Empty;
        private readonly List<IControllerInputDevice> _inputDevices;

        private readonly Dictionary<Guid, PresetConfiguration> _presetConfigurationForCompositions =
            new Dictionary<Guid, PresetConfiguration>();

        //public Instance ActiveComposition;

        public void DrawInputContextMenu(IInputSlot inputSlot, SymbolUi compositionUi, SymbolChildUi symbolChildUi)
        {
            // Save relevant creation details
            _nextCompositionUi = compositionUi;
            _nextSymbolChildUi = symbolChildUi;
            _nextInputSlotFor = inputSlot;
            _nextNameFor = symbolChildUi.SymbolChild.ReadableName;

            CustomComponents.HintLabel("Group");
            if (ConfigForActiveComposition != null)
            {
                foreach (var group in ConfigForActiveComposition.ParameterGroups)
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
                                CreateNewParameterForActiveGroup(parameterIndex);

                            ImGui.PopID();
                        }

                        ImGui.EndMenu();
                    }

                    ImGui.PopID();
                }
            }

            if (ImGui.MenuItem("+ Add Group"))
            {
                GetOrCreateActiveConfiguration();
                //T3Ui.PresetSystem.CreateNewGroupForInput();

                AddGroupDialog.ShowNextFrame();
            }

            ImGui.EndMenu();
        }

        #region trigger commands 
        public void ActivateGroupAtIndex(int index)
        {
            var activeConfig = ConfigForActiveComposition;
            if (activeConfig == null)
                return;

            if (activeConfig.ParameterGroups.Count <= index)
            {
                Log.Warning($"Tried activate group at {index}. There are only {activeConfig.ParameterGroups.Count} defined.");
                return;
            }

            activeConfig.ActiveGroupId = activeConfig.ParameterGroups[index].Id;
        }
        #endregion
        
        private SymbolUi _nextCompositionUi;
        private SymbolChildUi _nextSymbolChildUi;
        private IInputSlot _nextInputSlotFor;
        private string _nextNameFor;
        private static readonly AddGroupDialog AddGroupDialog = new AddGroupDialog();

    }

    public interface IControllerInputDevice
    {
        void Update(PresetSystem presetSystem, MidiIn midiIn, PresetConfiguration config);
        int GetProductNameHash();
    }
}