using System;
using System.Collections.Generic;
using T3.Core.Operator;
using T3.Gui.Graph;
using T3.Gui.Graph.Interaction;
using T3.Gui.Interaction.Variations.Model;
using T3.Gui.Selection;

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
    }
}