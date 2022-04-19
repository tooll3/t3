using System;
using System.Collections.Generic;
using T3.Core.Operator;
using T3.Gui.Graph;
using t3.Gui.Interaction.Presets.Model;
using T3.Gui.Selection;

namespace t3.Gui.Interaction.Presets
{
    /// <summary>
    /// Handles the live integration of presets model to the user interface
    /// </summary>
    public static class PresetHandling
    {
        public static SymbolVariationPool ActiveCompositionVariationPool { get; private set; }
        public static SymbolVariationPool ActiveInstancePresetPool { get; private set; }
        
        public static Instance ActiveInstanceForVariations  { get; private set; }
        public static Instance ActiveInstanceForPresets  { get; private set; }

        
        /// <summary>
        /// Update variation handling
        /// </summary>
        public static void Update()
        {
            // Sync with composition selected in UI
            var primaryGraphWindow = GraphWindow.GetPrimaryGraphWindow();
            if (primaryGraphWindow == null)
                return;

            var singleSelectedInstance = SelectionManager.GetSelectedInstance();
            if (singleSelectedInstance != null)
            {
                var selectedSymbolId = singleSelectedInstance.Symbol.Id;
                ActiveInstancePresetPool = GetOrLoadVariations(selectedSymbolId);
                ActiveCompositionVariationPool = GetOrLoadVariations(singleSelectedInstance.Parent.Symbol.Id);
                ActiveInstanceForPresets = singleSelectedInstance;
                ActiveInstanceForVariations = singleSelectedInstance.Parent;
            }
            else
            {
                ActiveInstancePresetPool = null;
                
                var activeCompositionInstance = primaryGraphWindow.GraphCanvas.CompositionOp;
                ActiveInstanceForVariations = activeCompositionInstance;
                
                // Prevent variations for library operators
                if (activeCompositionInstance.Symbol.Namespace.StartsWith("lib."))
                {
                    ActiveCompositionVariationPool = null;
                }
                else
                {
                    ActiveCompositionVariationPool = GetOrLoadVariations(activeCompositionInstance.Symbol.Id);
                }

                if (!SelectionManager.IsAnythingSelected())
                {
                    ActiveInstanceForPresets = ActiveInstanceForVariations;
                }
            }
        }

        
        private static SymbolVariationPool GetOrLoadVariations(Guid symbolId)
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
    }
}