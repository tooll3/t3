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
                                    new NanoControl8(),
                                    new ApcMiniDevice(),
                                };

        }

        public void Update()
        {
            var primaryGraphWindow = GraphWindow.GetVisibleInstances().FirstOrDefault();
            if (primaryGraphWindow == null)
                return;

            _activeCompositionId = primaryGraphWindow._graphCanvas.CompositionOp.Symbol.Id;
            _contextForCompositions.TryGetValue(_activeCompositionId, out var activeContext);

            foreach (var inputDevice in _inputDevices)
            {
                // TODO: support generic input controllers with arbitrary DeviceId 
                var midiIn = MidiInConnectionManager.GetMidiInForProductNameHash(inputDevice.GetProductNameHash());
                if (midiIn == null)
                    continue;

                inputDevice.Update(this, midiIn, activeContext);
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

            SetOrCreateContextForActiveComposition();
            var group = ActiveContext.CreateNewGroup(_nextNameFor);
            group.AddParameterToIndex(CreateParameter(), 0);
            ActiveContext.ActiveGroupId = group.Id;
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
            SetOrCreateContextForActiveComposition();
            var activeGroup = ActiveContext.ActiveGroup;
            if (activeGroup == null)
            {
                Log.Warning("Can't save parameter without active group");
                return;
            }

            activeGroup.AddParameterToIndex(CreateParameter(), index);
        }

        private void SetOrCreateContextForActiveComposition()
        {
            if (_contextForCompositions.TryGetValue(_activeCompositionId, out var context))
            {
                ActiveContext = context;
                return;
            }

            ActiveContext = new PresetContext()
                                 {
                                     CompositionId = _activeCompositionId,
                                 };
            _contextForCompositions[_activeCompositionId] = ActiveContext;
        }

        private PresetScene GetOrCreateActiveScene()
        {
            return null;
        }

        private Guid _activeCompositionId = Guid.Empty;
        private readonly List<IControllerInputDevice> _inputDevices;

        private readonly Dictionary<Guid, PresetContext> _contextForCompositions =
            new Dictionary<Guid, PresetContext>();

        //public Instance ActiveComposition;

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
                foreach (var group in ActiveContext.ParameterGroups)
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
                SetOrCreateContextForActiveComposition();
                AddGroupDialog.ShowNextFrame();
            }

            ImGui.EndMenu();
        }

        #region trigger commands
        public void ActivateGroupAtIndex(int index)
        {
            if (ActiveContext == null)
                return;

            if (ActiveContext.ParameterGroups.Count <= index)
            {
                Log.Warning($"Tried activate group at {index}. There are only {ActiveContext.ParameterGroups.Count} defined.");
                return;
            }

            ActiveContext.ActiveGroupId = ActiveContext.ParameterGroups[index].Id;
        }
        
        public void SavePresetAtIndex(Preset preset, int buttonRangeIndex)
        {
            if (ActiveContext == null)
            {
                Log.Error($"Can't execute SavePresetAtIndex without valid context");
                return;
            }

            var address = new PresetAddress(buttonRangeIndex % 8, buttonRangeIndex / 8);

        }
        
        public void AppPresetAtIndex(int index)
        {
            //throw new NotImplementedException();
        }
        
        #endregion

        /// <summary>
        /// Is only changes by explicity user actions:
        /// - switching to a composition with a preset context
        /// - creating a context (e.g. by added parameters to blending)
        /// - switching e.g. with the midi controllers 
        /// </summary>
        public PresetContext ActiveContext { get; private set; }

        private SymbolUi _nextCompositionUi;
        private SymbolChildUi _nextSymbolChildUi;
        private IInputSlot _nextInputSlotFor;
        private string _nextNameFor;
        private static readonly AddGroupDialog AddGroupDialog = new AddGroupDialog();

        
    }

    public interface IControllerInputDevice
    {
        void Update(PresetSystem presetSystem, MidiIn midiIn, PresetContext context);
        int GetProductNameHash();
    }
}