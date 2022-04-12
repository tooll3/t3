using System;
using System.Collections.Generic;
using T3.Gui.Graph;
using t3.Gui.Interaction.Presets.Model;
//using T3.Gui.Interaction.Variation.Model;

namespace t3.Gui.Interaction.Presets
{
    /// <summary>
    /// Handles the live integration of presets model to the user interface
    /// </summary>
    public class PresetHandling
    {
        public void Update()
        {
            // Sync with composition selected in UI
            var primaryGraphWindow = GraphWindow.GetPrimaryGraphWindow();
            if (primaryGraphWindow == null)
                return;

            var activeCompositionInstance = primaryGraphWindow.GraphCanvas.CompositionOp;
            _activeCompositionId = activeCompositionInstance.Symbol.Id;
            _variationPoolForOperators.TryGetValue(_activeCompositionId, out var variationForComposition);

            // Attempt to read settings for composition
            if (variationForComposition != null)
            {
                ActivePool = variationForComposition;
                ActivePool.SymbolId = activeCompositionInstance.Symbol.Id;
            }
            else
            {
                if (_activeCompositionId != _lastCompositionId)
                {
                    var newOpVariation = SymbolVariationPool.InitVariationPoolForSymbol(_activeCompositionId);
                    if (newOpVariation != null)
                    {
                        _variationPoolForOperators[_activeCompositionId] = newOpVariation;
                        ActivePool = newOpVariation;
                        //ActivePool.CompositionInstance = activeCompositionInstance;
                    }
                    else
                    {
                        _lastCompositionId = _activeCompositionId;
                    }
                }
            }

            // Update active op variation
            // if (ActivePool != null)
            // {
            //     // Check for auto updates from animated or driven Operators
            //     if (ActivatePresets.BlendSettingForCompositionIds.TryGetValue(ActivePool.CompositionId, out var blendSetting))
            //     {
            //         if (blendSetting.WasActivatedLastFrame)
            //         {
            //             Log.Debug("Blend setting was updated");
            //             if (ActivePool.TryGetGroup(blendSetting.GroupIndex, out var group))
            //             {
            //                 if (group != ActivePool.ActiveGroup)
            //                 {
            //                     ActivateGroupAtIndex(blendSetting.GroupIndex);
            //                 }
            //
            //                 //ActivateOrCreatePresetAtIndex(blendSetting.PresetAIndex);
            //                 ActivePool.TryActivatePresetAtAddress(new PresetAddress(blendSetting.GroupIndex, blendSetting.PresetAIndex));
            //                 blendSetting.WasActivatedLastFrame = false;
            //             }
            //         }
            //     }
            //
            //     foreach (var group in ActivePool.Groups)
            //     {
            //         ActivePool.UpdateBlendTransition(group);
            //     }
            //     ActivePool.UpdateInputReferences();
            // }

            // Update Midi Devices 
            // foreach (var connectedDevice in _inputDevices)
            // {
            //     // TODO: support generic input controllers with arbitrary DeviceId 
            //     var midiIn = MidiInConnectionManager.GetMidiInForProductNameHash(connectedDevice.GetProductNameHash());
            //     if (midiIn == null)
            //         continue;
            //
            //     connectedDevice.Update(this, midiIn, ActivePool);
            // }

            // Draw Ui
            //AddGroupDialog.Draw(ref _nextName);
        }
        
        private Guid _activeCompositionId = Guid.Empty;
        private Guid _lastCompositionId;
        //private readonly List<IControllerInputDevice> _inputDevices;
        public SymbolVariationPool ActivePool { get; private set; }

        private readonly Dictionary<Guid, SymbolVariationPool> _variationPoolForOperators = new Dictionary<Guid, SymbolVariationPool>();

    }
}