using System;
using System.Collections.Generic;
using System.Linq;
using Operators.Utils;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Graph;
using T3.Gui.Graph.Interaction;
using T3.Gui.Interaction.Variations.Midi;
using T3.Gui.Interaction.Variations.Model;
using T3.Gui.UiHelpers;
using T3.Gui.Windows.Variations;

namespace T3.Gui.Interaction.Variations
{
    /// <summary>
    /// Handles the live integration of variation model to the user interface.
    /// </summary>
    /// <remarks>
    /// Variations are a sets of symbolChild.input-parameters combinations defined for an Symbol.
    /// These input slots can also include the symbols out inputs which thus can be used for defining
    /// and applying "presets" to instances of that symbol.
    ///
    /// Most variations will modify(!) the symbol, is great while working within a single symbol
    /// and tweaking an blending parameters. However it's potentially unintended (or dangerous) if many instances
    /// of the modified exist. That's why applying symbol-variations is restricted for Symbols in the lib-namespace.  
    /// </remarks>
    public static class VariationHandling
    {
        public static SymbolVariationPool ActivePoolForVariations { get; private set; }
        public static SymbolVariationPool ActivePoolForPresets { get; private set; }
        
        public static Instance ActiveInstanceForVariations  { get; private set; }
        public static Instance ActiveInstanceForPresets  { get; private set; }

        public static void Init()
        {
            // Scan for output devices (e.g. to update LEDs etc.)
            MidiOutConnectionManager.Init();

            _inputDevices = new List<IControllerInputDevice>()
                                {
                                    new Apc40Mk2(),
                                    new NanoControl8(),
                                    new ApcMini(),
                                };
        }
        private static List<IControllerInputDevice> _inputDevices;
        
        /// <summary>
        /// Update variation handling
        /// </summary>
        public static void Update()
        {
            // Sync with composition selected in UI
            var primaryGraphWindow = GraphWindow.GetPrimaryGraphWindow();
            if (primaryGraphWindow == null)
                return;


            var singleSelectedInstance = NodeSelection.GetSelectedInstance();
            if (singleSelectedInstance != null)
            {
                var selectedSymbolId = singleSelectedInstance.Symbol.Id;
                ActivePoolForPresets = GetOrLoadVariations(selectedSymbolId);
                ActivePoolForVariations = GetOrLoadVariations(singleSelectedInstance.Parent.Symbol.Id);
                ActiveInstanceForPresets = singleSelectedInstance;
                ActiveInstanceForVariations = singleSelectedInstance.Parent;
            }
            else
            {
                ActivePoolForPresets = null;
                
                var activeCompositionInstance = primaryGraphWindow.GraphCanvas.CompositionOp;
                ActiveInstanceForVariations = activeCompositionInstance;
                
                // Prevent variations for library operators
                if (activeCompositionInstance.Symbol.Namespace.StartsWith("lib."))
                {
                    ActivePoolForVariations = null;
                }
                else
                {
                    ActivePoolForVariations = GetOrLoadVariations(activeCompositionInstance.Symbol.Id);
                }

                if (!NodeSelection.IsAnythingSelected())
                {
                    ActiveInstanceForPresets = ActiveInstanceForVariations;
                }
            }
            
            UpdateMidiDevices();
        }

        private static void UpdateMidiDevices()
        {
            // Update Midi Devices 
            foreach (var connectedDevice in _inputDevices)
            {
                // TODO: support generic input controllers with arbitrary DeviceId 
                var midiIn = MidiInConnectionManager.GetMidiInForProductNameHash(connectedDevice.GetProductNameHash());
                if (midiIn == null)
                    continue;

                if (ActivePoolForVariations != null)
                {
                    connectedDevice.Update(midiIn, ActivePoolForVariations.ActiveVariation);
                }
            }
        }
        
        

        public static SymbolVariationPool GetOrLoadVariations(Guid symbolId)
        {
            if (_variationPoolForOperators.TryGetValue(symbolId, out var variationForComposition))
            {
                return variationForComposition;
            }

            var newOpVariation = SymbolVariationPool.InitVariationPoolForSymbol(symbolId);
            _variationPoolForOperators[newOpVariation.SymbolId] = newOpVariation;
            return newOpVariation;
        }


        private static readonly Dictionary<Guid, SymbolVariationPool> _variationPoolForOperators = new();

        public static void ActivateOrCreatePresetAtIndex(int activationIndex)
        {
            if (ActivePoolForVariations == null)
            {
                Log.Warning($"Can't save variation #{activationIndex}. No variation pool active.");
                return;
            }
            
            if(TryGetVariation(activationIndex, out var existingVariation))
            {
                ActivePoolForVariations.Apply(ActiveInstanceForVariations, existingVariation, UserSettings.Config.PresetsResetToDefaultValues);
                return;
            } 
            
            SaveVariationForSelectedOperators(activationIndex);
        }

        public static void SavePresetAtIndex(int activationIndex)
        {
            if (ActivePoolForVariations == null)
            {
                Log.Warning($"Can't save variation #{activationIndex}. No variation pool active.");
                return;
            }

            SaveVariationForSelectedOperators(activationIndex);
        }

        public static void RemovePresetAtIndex(int obj)
        {
            Log.Warning($"RemovePresetAtIndex {obj} not implemented");

        }

        public static void StartBlendingPresets(int[] indices)
        {
            Log.Warning($"StartBlendingPresets {indices} not implemented");
        }

        public static void BlendValuesUpdate(int obj)
        {
            Log.Warning($"BlendValuesUpdate {obj} not implemented");
        }

        public static void AppendPresetToCurrentGroup(int obj)
        {
            Log.Warning($"AppendPresetToCurrentGroup {obj} not implemented");
        }

        public static bool TryGetVariation(int activationIndex, out Variation variation)
        {
            variation = null;
            if (ActivePoolForVariations == null)
                return false;
            
            foreach (var v in ActivePoolForVariations.Variations)
            {
                if (v.ActivationIndex != activationIndex)
                    continue;
                    
                variation = v;
                return true;
            }

            return false;
        }

        private const int AutoIndex=-1;
        public static Variation SaveVariationForSelectedOperators(int activationIndex = AutoIndex )
        {
            if (ActivePoolForVariations == null)
                return null;
            
            if (activationIndex != AutoIndex)
            {
                if(TryGetVariation(activationIndex, out var existingVariation))
                {
                    ActivePoolForVariations.DeleteVariation(existingVariation);
                } 
            }
            
            var selectedInstances = NodeSelection.GetSelectedInstances().ToList();
            var newVariation = ActivePoolForVariations.CreateVariationForCompositionInstances(selectedInstances);
            if (newVariation == null)
                return null;
            
            newVariation.PosOnCanvas = VariationBaseCanvas.FindFreePositionForNewThumbnail(VariationHandling.ActivePoolForVariations.Variations);
            if (activationIndex != AutoIndex)
                newVariation.ActivationIndex = activationIndex;
                
            VariationThumbnail.VariationForRenaming = newVariation;
            return newVariation;

        }
    }
}