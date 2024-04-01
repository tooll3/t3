using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Interaction.Variations;
using T3.Editor.Gui.Interaction.Variations.Model;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.Windows.Variations
{
    public class PresetCanvas : VariationBaseCanvas
    {
        public virtual void DrawToolbarFunctions()
        {
            var s = ImGui.GetFrameHeight();
            if (VariationHandling.ActivePoolForPresets == null)
                return;
            
            if (CustomComponents.IconButton(Icon.Plus, new Vector2(s, s)))
            {
                CreateVariation();
            }
        }

        protected override string GetTitle()
        {
            if (VariationHandling.ActiveInstanceForPresets == null)
                return "";
            
            return $"...for {VariationHandling.ActiveInstanceForPresets?.Symbol.Name}";
        }

        protected override Instance InstanceForBlendOperations => VariationHandling.ActiveInstanceForPresets;
        protected override SymbolVariationPool PoolForBlendOperations => VariationHandling.ActivePoolForPresets;

        protected override void DrawAdditionalContextMenuContent()
        {
        }

        public virtual Variation CreateVariation()
        {
            var newVariation = VariationHandling.ActivePoolForPresets.CreatePresetForInstanceSymbol(VariationHandling.ActiveInstanceForPresets);
            if (newVariation != null)
            {
                newVariation.PosOnCanvas = VariationBaseCanvas.FindFreePositionForNewThumbnail(VariationHandling.ActivePoolForPresets.Variations);
                VariationThumbnail.VariationForRenaming = newVariation;
                VariationHandling.ActivePoolForPresets.SaveVariationsToFile();
            }

            Selection.SetSelection(newVariation);
            ResetView();
            TriggerThumbnailUpdate();
            return newVariation;
        }
    }
}