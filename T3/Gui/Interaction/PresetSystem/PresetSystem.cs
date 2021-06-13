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
using T3.Gui.Styling;
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
        public void Update()
        {
            // Sync with composition selected in UI
            var primaryGraphWindow = GraphWindow.GetVisibleInstances().FirstOrDefault();
            if (primaryGraphWindow == null)
                return;
            
            var activeCompositionInstance = primaryGraphWindow.GraphCanvas.CompositionOp;
            _activeCompositionId = activeCompositionInstance.Symbol.Id;
            _contextForCompositions.TryGetValue(_activeCompositionId, out var contextForCurrentComposition);

            // Attempt to read settings for composition
            if (contextForCurrentComposition != null)
            {
                ActiveContext = contextForCurrentComposition;
                ActiveContext.CompositionInstance = activeCompositionInstance;
            }
            else
            {
                if (_activeCompositionId != _lastCompositionId)
                {
                    var newContext = CompositionContext.ReadFromJson(_activeCompositionId);
                    if (newContext != null)
                    {
                        _contextForCompositions[_activeCompositionId] = newContext;
                        ActiveContext = newContext;
                        ActiveContext.CompositionInstance = activeCompositionInstance;
                    }
                    else
                    {
                        _lastCompositionId = _activeCompositionId;
                    }
                }
            }

            // Update active context
            if (ActiveContext != null)
            {
                if (ActivatePresets.BlendSettingForCompositionIds.TryGetValue(ActiveContext.CompositionId, out var blendSetting))
                {
                    if (blendSetting.WasActivatedLastFrame)
                    {
                        Log.Debug("Blend setting was updated");
                        if (ActiveContext.TryGetGroup(blendSetting.GroupIndex, out var group))
                        {
                            if (group != ActiveContext.ActiveGroup)
                            {
                                ActivateGroupAtIndex(blendSetting.GroupIndex);
                            }

                            //ActivateOrCreatePresetAtIndex(blendSetting.PresetAIndex);
                            ActiveContext.TryActivatePresetAtAddress(new PresetAddress(blendSetting.GroupIndex, blendSetting.PresetAIndex));
                            blendSetting.WasActivatedLastFrame = false;
                        }
                    }
                }
                
                ActiveContext.UpdateInputReferences();
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
                    if (ImGui.MenuItem("Append to G" + (activeGroup.Index + 1)))
                    {
                        var index = activeGroup.FindNextFreeParameterIndex();
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
        
        
        internal void CreateNewGroupForInput()
        {
            SetOrCreateContextForActiveComposition();
            
            var group = ActiveContext.AppendNewGroup(_nextNameFor);
            group.AddParameterToIndex(new GroupParameter
                                          {
                                              Id = Guid.NewGuid(),
                                              SymbolChildId = _nextSymbolChildUi.Id,
                                              InputId = _nextInputSlotFor.Id,
                                              Title = _nextSymbolChildUi.SymbolChild.ReadableName + "." + _nextInputSlotFor.Input.Name,
                                          }, 0);
            ActiveContext.SetGroupAsActive(group);
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

            var newParameter = new GroupParameter
                                   {
                                       Id = Guid.NewGuid(),
                                       SymbolChildId = _nextSymbolChildUi.Id,
                                       InputId = _nextInputSlotFor.Id,
                                       Title = _nextSymbolChildUi.SymbolChild.ReadableName + "." + _nextInputSlotFor.Input.Name,
                                   };
            activeGroup.AddParameterToIndex(newParameter, parameterIndex);
            
            var instance = ActiveContext.CompositionInstance.Children.SingleOrDefault(c => c.SymbolChildId == newParameter.SymbolChildId);
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
                var instance = ActiveContext.CompositionInstance.Children.SingleOrDefault(child => child.SymbolChildId == parameter.SymbolChildId);
                if (symbolChildUi != null && instance != null)
                {
                    SelectionManager.AddSymbolChildToSelection(symbolChildUi, instance);
                }
            }

            FitViewToSelectionHandling.FitViewToSelection();
        }
        
        
        public void ActivateGroupAtIndex(int index)
        {
            if (ActiveContext == null)
                return;

            var focusSelection = ActiveContext.ActivateGroupAtIndex(index);
            if(focusSelection)
                SelectUiElementsForGroup(ActiveContext.ActiveGroup);
            
        }
        
        //---------------------------------------------------------------------------------
        #region API calls from midi inputs
        public void SavePresetAtIndex(int buttonRangeIndex)
        {
            if (ActiveContext == null)
            {
                Log.Error($"Can't execute SavePresetAtIndex without valid context");
                return;
            }

            ActiveContext.SavePresetAtIndex(buttonRangeIndex);
        }
        
        

        public void ActivateOrCreatePresetAtIndex(int buttonRangeIndex)
        {
            if (ActiveContext == null)
            {
                Log.Error($"Can't execute ApplyPresetAtIndex without valid context");
                return;
            }
            
            ActiveContext.ActivateOrCreatePresetAtIndex(buttonRangeIndex);
        }

        public void RemovePresetAtIndex(int buttonRangeIndex)
        {
            if (ActiveContext == null)
            {
                Log.Error($"Can't execute ApplyPresetAtIndex without valid context");
                return;
            }

            ActiveContext.RemovePresetAtIndex(buttonRangeIndex);
        }

        public void StartBlendingPresets(int[] indices)
        {
            Log.Debug(" Start blending " + String.Join(", ", indices));
            ActiveContext?.StartBlendingPresets(indices);
        }
        
        internal void BlendValuesUpdate(int groupIndex, float value)
        {
            ActiveContext?.BlendValuesUpdate(groupIndex, value);
        }

        public void AppendPresetToCurrentGroup()
        {
            ActiveContext?.AppendPresetToCurrentGroup();
        }
        

        #endregion

        private Guid _activeCompositionId = Guid.Empty;
        private readonly List<IControllerInputDevice> _inputDevices;

        private readonly Dictionary<Guid, CompositionContext> _contextForCompositions = new Dictionary<Guid, CompositionContext>();


        /// <summary>
        /// Only changes by explicit user actions:
        /// - switching to a composition with a preset context
        /// - creating a context (e.g. by added parameters to blending)
        /// - switching e.g. with the midi controllers 
        /// </summary>
        public CompositionContext ActiveContext { get; private set; }

        private Guid _lastCompositionId;

        private SymbolChildUi _nextSymbolChildUi;
        private IInputSlot _nextInputSlotFor;
        private string _nextNameFor;

        private static readonly AddGroupDialog AddGroupDialog = new AddGroupDialog();
    }
}