using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Operators.Utils;
using T3.Core.Logging;
using T3.Core.Operator.Slots;
using T3.Gui.Graph;
using T3.Gui.Graph.Interaction;
using T3.Gui.Interaction.LegacyVariations.Dialogs;
using T3.Gui.Interaction.LegacyVariations.Midi;
using T3.Gui.Interaction.LegacyVariations.Model;
using T3.Gui.Selection;
//using T3.Operators.Types.Id_59a0458e_2f3a_4856_96cd_32936f783cc5;
using T3.Operators.Types.Id_a53f3873_a5aa_4bcc_aa06_0745d98209d6;

namespace T3.Gui.Interaction.LegacyVariations
{
    public class LegacyVariationHandling
    {
        public LegacyVariationHandling()
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

        public void Update()
        {
            // Sync with composition selected in UI
            var primaryGraphWindow = GraphWindow.GetPrimaryGraphWindow();
            if (primaryGraphWindow == null)
                return;

            var activeCompositionInstance = primaryGraphWindow.GraphCanvas.CompositionOp;
            _activeCompositionId = activeCompositionInstance.Symbol.Id;
            _variationForOperators.TryGetValue(_activeCompositionId, out var variationForComposition);

            // Attempt to read settings for composition
            if (variationForComposition != null)
            {
                ActiveOperatorVariation = variationForComposition;
                ActiveOperatorVariation.CompositionInstance = activeCompositionInstance;
            }
            else
            {
                if (_activeCompositionId != _lastCompositionId)
                {
                    var newOpVariation = OperatorVariation.ReadFromJson(_activeCompositionId);
                    if (newOpVariation != null)
                    {
                        _variationForOperators[_activeCompositionId] = newOpVariation;
                        ActiveOperatorVariation = newOpVariation;
                        ActiveOperatorVariation.CompositionInstance = activeCompositionInstance;
                    }
                    else
                    {
                        _lastCompositionId = _activeCompositionId;
                    }
                }
            }

            // Update active op variation
            if (ActiveOperatorVariation != null)
            {
                // Check for auto updates from animated or driven Operators
                if (ActivatePresets.BlendSettingForCompositionIds.TryGetValue(ActiveOperatorVariation.CompositionId, out var blendSetting))
                {
                    if (blendSetting.WasActivatedLastFrame)
                    {
                        Log.Debug("Blend setting was updated");
                        if (ActiveOperatorVariation.TryGetGroup(blendSetting.GroupIndex, out var group))
                        {
                            if (group != ActiveOperatorVariation.ActiveGroup)
                            {
                                ActivateGroupAtIndex(blendSetting.GroupIndex);
                            }

                            //ActivateOrCreatePresetAtIndex(blendSetting.PresetAIndex);
                            ActiveOperatorVariation.TryActivatePresetAtAddress(new PresetAddress(blendSetting.GroupIndex, blendSetting.PresetAIndex));
                            blendSetting.WasActivatedLastFrame = false;
                        }
                    }
                }

                foreach (var group in ActiveOperatorVariation.Groups)
                {
                    ActiveOperatorVariation.UpdateBlendTransition(group);
                }
                ActiveOperatorVariation.UpdateInputReferences();
            }

            // Update Midi Devices 
            foreach (var connectedDevice in _inputDevices)
            {
                // TODO: support generic input controllers with arbitrary DeviceId 
                var midiIn = MidiInConnectionManager.GetMidiInForProductNameHash(connectedDevice.GetProductNameHash());
                if (midiIn == null)
                    continue;

                connectedDevice.Update(this, midiIn, ActiveOperatorVariation);
            }

            // Draw Ui
            AddGroupDialog.Draw(ref _nextName);
        }

        public void DrawInputContextMenu(IInputSlot inputSlot, SymbolUi compositionUi, SymbolChildUi symbolChildUi)
        {
            // Save relevant creation details
            _nextSymbolChildUi = symbolChildUi;
            _nextInputSlot = inputSlot;
            _nextName = symbolChildUi.SymbolChild.ReadableName;

            CustomComponents.HintLabel("Group");
            if (ActiveOperatorVariation != null)
            {
                foreach (var group in ActiveOperatorVariation.Groups)
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
                                ActiveOperatorVariation.SetGroupAsActive(group);
                                CreateNewParameterForActiveGroup(parameterIndex);
                            }

                            ImGui.PopID();
                        }

                        ImGui.EndMenu();
                    }

                    ImGui.PopID();
                }

                var activeGroup = ActiveOperatorVariation?.ActiveGroup;
                if (activeGroup != null)
                {
                    if (ImGui.MenuItem("Append to G" + (activeGroup.Index + 1)))
                    {
                        var index = activeGroup.FindNextFreeParameterIndex();
                        CreateNewParameterForActiveGroup(index);
                    }
                }
            }

            if (ImGui.MenuItem("+ Add Group"))
            {
                SetOrCreateVariationsForActiveComposition();
                AddGroupDialog.ShowNextFrame();
            }

            ImGui.EndMenu();
        }

        internal void CreateNewGroupForInput()
        {
            SetOrCreateVariationsForActiveComposition();

            var group = ActiveOperatorVariation.AppendNewGroup(_nextName);

            if (_nextSymbolChildUi != null && _nextInputSlot != null)
            {
                group.AddParameterToIndex(new GroupParameter
                                              {
                                                  Id = Guid.NewGuid(),
                                                  SymbolChildId = _nextSymbolChildUi.Id,
                                                  InputId = _nextInputSlot.Id,
                                                  Title = _nextSymbolChildUi.SymbolChild.ReadableName + "." + _nextInputSlot.Input.Name,
                                              }, 0);
            }
            ActiveOperatorVariation.SetGroupAsActive(group);
        }

        private void CreateNewParameterForActiveGroup(int parameterIndex)
        {
            SetOrCreateVariationsForActiveComposition();
            var activeGroup = ActiveOperatorVariation.ActiveGroup;
            if (activeGroup == null)
            {
                Log.Warning("Can't save parameter without active group");
                return;
            }

            var newParameter = new GroupParameter
                                   {
                                       Id = Guid.NewGuid(),
                                       SymbolChildId = _nextSymbolChildUi.Id,
                                       InputId = _nextInputSlot.Id,
                                       Title = _nextSymbolChildUi.SymbolChild.ReadableName + "." + _nextInputSlot.Input.Name,
                                   };
            activeGroup.AddParameterToIndex(newParameter, parameterIndex);

            var instance = ActiveOperatorVariation.CompositionInstance.Children.SingleOrDefault(c => c.SymbolChildId == newParameter.SymbolChildId);
            if (instance == null)
            {
                Log.Warning("Can't find correct instance of parameter view");
                return;
            }

            var input = instance.Inputs.Single(inp => inp.Id == newParameter.InputId);
            foreach (var preset in ActiveOperatorVariation.GetPresetsForGroup(activeGroup))
            {
                preset.ValuesForGroupParameterIds[newParameter.Id] = input.Input.Value.Clone();
            }
        }

        private void SetOrCreateVariationsForActiveComposition()
        {
            if (_variationForOperators.TryGetValue(_activeCompositionId, out var existingOpVariation))
            {
                ActiveOperatorVariation = existingOpVariation;
                return;
            }

            ActiveOperatorVariation = new OperatorVariation()
                                          {
                                              CompositionId = _activeCompositionId,
                                          };
            _variationForOperators[_activeCompositionId] = ActiveOperatorVariation;
        }

        private void SelectUiElementsForGroup(ParameterGroup group)
        {
            if (ActiveOperatorVariation == null)
                return;

            NodeSelection.Clear();

            if (!SymbolUiRegistry.Entries.TryGetValue(_activeCompositionId, out var symbolUi))
                return;

            foreach (var parameter in @group.Parameters)
            {
                if (parameter == null)
                    continue;

                var symbolChildUi = symbolUi.ChildUis.SingleOrDefault(childUi => childUi.Id == parameter.SymbolChildId);
                var instance = ActiveOperatorVariation.CompositionInstance.Children.SingleOrDefault(child => child.SymbolChildId == parameter.SymbolChildId);
                if (symbolChildUi != null && instance != null)
                {
                    NodeSelection.AddSymbolChildToSelection(symbolChildUi, instance);
                }
            }

            FitViewToSelectionHandling.FitViewToSelection();
        }

        public void ActivateGroupAtIndex(int index)
        {
            if (ActiveOperatorVariation == null)
                return;

            var focusSelection = ActiveOperatorVariation.ActivateGroupAtIndex(index);
            if (focusSelection)
                SelectUiElementsForGroup(ActiveOperatorVariation.ActiveGroup);
        }

        //---------------------------------------------------------------------------------
        #region API calls from midi inputs
        public void ShowAddGroupDialog()
        {
            _nextSymbolChildUi = null;
            _nextInputSlot = null;
            _nextName = "Group";
            SetOrCreateVariationsForActiveComposition();
            AddGroupDialog.ShowNextFrame();
        }
        
        
        public void SavePresetAtIndex(int buttonRangeIndex)
        {
            if (ActiveOperatorVariation == null)
            {
                Log.Error($"Can't execute SavePresetAtIndex without valid operator variation");
                return;
            }

            ActiveOperatorVariation.SavePresetAtIndex(buttonRangeIndex);
        }

        public void ActivateOrCreatePresetAtIndex(int buttonRangeIndex)
        {
            if (ActiveOperatorVariation == null)
            {
                Log.Error($"Can't execute ApplyPresetAtIndex without valid operator variation");
                return;
            }

            ActiveOperatorVariation.ActivateOrCreatePresetAtIndex(buttonRangeIndex);
        }

        public void RemovePresetAtIndex(int buttonRangeIndex)
        {
            if (ActiveOperatorVariation == null)
            {
                Log.Error($"Can't execute ApplyPresetAtIndex without valid operator variation");
                return;
            }

            ActiveOperatorVariation.RemovePresetAtIndex(buttonRangeIndex);
        }

        public void StartBlendingPresets(int[] indices)
        {
            Log.Debug(" Start blending " + String.Join(", ", indices));
            ActiveOperatorVariation?.StartBlendingPresets(indices);
        }

        internal void BlendValuesUpdate(int groupIndex, float value)
        {
            ActiveOperatorVariation?.BlendValuesUpdate(groupIndex, value);
        }

        public void AppendPresetToCurrentGroup()
        {
            ActiveOperatorVariation?.AppendPresetToCurrentGroup();
        }
        #endregion

        private Guid _activeCompositionId = Guid.Empty;
        private readonly List<IControllerInputDevice> _inputDevices;

        private readonly Dictionary<Guid, OperatorVariation> _variationForOperators = new Dictionary<Guid, OperatorVariation>();

        /// <summary>
        /// Only changes by explicit user actions:
        /// - switching to a composition with operator variations
        /// - creating a context (e.g. by added parameters to blending)
        /// - switching e.g. with the midi controllers 
        /// </summary>
        public OperatorVariation ActiveOperatorVariation { get; private set; }

        private Guid _lastCompositionId;

        private SymbolChildUi _nextSymbolChildUi;
        private IInputSlot _nextInputSlot;
        private string _nextName;

        private static readonly AddGroupDialog AddGroupDialog = new AddGroupDialog();
    }
}