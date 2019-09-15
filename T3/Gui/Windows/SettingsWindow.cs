using ImGuiNET;
using System.Numerics;
using System.Text;
using T3.Core.Logging;
using T3.Gui.Graph;
using T3.Gui.TypeColors;
using static T3.Gui.T3UI;

namespace T3.Gui.Windows
{
    public class SettingsWindow : Window
    {
        public SettingsWindow() : base()
        {
            _title = "Settings";
        }
        public static bool UseVSync => _vsync;

        private static bool _vsync = true;

        public static bool WindowRegionsVisible;
        public static bool ItemRegionsVisible;
        //public static bool ConsoleWindowVisible = true;
        //public static bool ParameterWindowVisible = true;

        protected override void DrawContent()
        {
            T3Metrics.Draw();

            ImGui.Text("Debug options...");
            ImGui.Checkbox("VSync", ref _vsync);
            ImGui.Checkbox("Show Window Regions", ref WindowRegionsVisible);
            ImGui.Checkbox("Show Item Regions", ref ItemRegionsVisible);

            if (ImGui.Button("New Parameter View"))
                OpenNewParameterView();

            ImGui.Text("Options");
            ColorVariations.DrawSettingsUi();
            if (ImGui.TreeNode("Styles"))
            {
                ImGui.DragFloat("Height Connection Zone", ref GraphOperator._usableSlotHeight);
                ImGui.DragFloat2("Label position", ref GraphOperator._labelPos);
                ImGui.DragFloat("Slot Gaps", ref GraphOperator._slotGaps, 0.1f, 0, 10f);
                ImGui.DragFloat("Input Slot Margin Y", ref GraphOperator._inputSlotMargin, 0.1f, 0, 10f);
                ImGui.DragFloat("Input Slot Height", ref GraphOperator._inputSlotHeight, 0.1f, 0, 10f);
                ImGui.DragFloat("Output Slot Margin", ref GraphOperator._outputSlotMargin, 0.1f, 0, 10f);
                ImGui.TreePop();
            }
            if (ImGui.TreeNode("ImGui Styles"))
                T3Style.DrawUi();
        }
    }
}