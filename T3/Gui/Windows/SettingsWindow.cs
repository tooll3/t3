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
                ImGui.DragFloat("Height Connection Zone", ref GraphNode._usableSlotThickness);
                ImGui.DragFloat2("Label position", ref GraphNode._labelPos);
                ImGui.DragFloat("Slot Gaps", ref GraphNode._slotGaps, 0.1f, 0, 10f);
                ImGui.DragFloat("Input Slot Margin Y", ref GraphNode._inputSlotMargin, 0.1f, 0, 10f);
                ImGui.DragFloat("Input Slot Height", ref GraphNode._inputSlotThickness, 0.1f, 0, 10f);
                ImGui.DragFloat("Output Slot Margin", ref GraphNode._outputSlotMargin, 0.1f, 0, 10f);
                ImGui.TreePop();
            }
            if (ImGui.TreeNode("ImGui Styles"))
                T3Style.DrawUi();
        }
    }
}