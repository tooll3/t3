using System.Collections.Generic;
using ImGuiNET;
using T3.Gui.Graph;
using T3.Gui.TypeColors;

namespace T3.Gui.Windows
{
    public class SettingsWindow : Window
    {
        public SettingsWindow() 
        {
            Config.Title = "Settings";
        }
        public static bool UseVSync => _vSync;

        private static bool _vSync = true;

        public static bool WindowRegionsVisible;
        public static bool ItemRegionsVisible;

        protected override void DrawContent()
        {
            T3Metrics.Draw();

            ImGui.Text("Debug options...");
            ImGui.Checkbox("VSync", ref _vSync);
            ImGui.Checkbox("Show Window Regions", ref WindowRegionsVisible);
            ImGui.Checkbox("Show Item Regions", ref ItemRegionsVisible);
            
            ImGui.Text("Options");
            ColorVariations.DrawSettingsUi();
            if (ImGui.TreeNode("Styles"))
            {
                ImGui.DragFloat("Height Connection Zone", ref GraphNode.UsableSlotThickness);
                ImGui.DragFloat2("Label position", ref GraphNode.LabelPos);
                ImGui.DragFloat("Slot Gaps", ref GraphNode.SlotGaps, 0.1f, 0, 10f);
                ImGui.DragFloat("Input Slot Margin Y", ref GraphNode.InputSlotMargin, 0.1f, 0, 10f);
                ImGui.DragFloat("Input Slot Thickness", ref GraphNode.InputSlotThickness, 0.1f, 0, 10f);
                ImGui.DragFloat("Output Slot Margin", ref GraphNode.OutputSlotMargin, 0.1f, 0, 10f);
                ImGui.TreePop();
            }
            if (ImGui.TreeNode("ImGui Styles"))
                T3Style.DrawUi();
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }
    }
}