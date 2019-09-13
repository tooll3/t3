using ImGuiNET;
using System.Text;
using T3.Core.Logging;
using T3.Gui.Graph;
using T3.Gui.TypeColors;
using static T3.Gui.T3UI;

namespace T3.Gui
{
    public class UiSettingsWindow
    {
        public static bool UseVSync => _vsync;
        private static bool _vsync = true;

        public static bool WindowRegionsVisible;
        public static bool ItemRegionsVisible;
        public static bool DemoWindowVisible;
        public static bool ConsoleWindowVisible = true;
        public static bool ParameterWindowVisible = true;
        public static bool ShowMetrics;

        public static unsafe void DrawUiSettings()
        {
            ImGui.Begin("Stats");
            {
                Metrics.Draw();
                ImGui.Checkbox("VSync", ref _vsync);
                ImGui.Checkbox("Show Window Regions", ref WindowRegionsVisible);
                ImGui.Checkbox("Show Metrics", ref ShowMetrics);
                ImGui.Checkbox("Show Item Regions", ref ItemRegionsVisible);
                ImGui.Checkbox("Demo Window Visible", ref DemoWindowVisible);
                ImGui.Checkbox("Console Window Visible", ref ConsoleWindowVisible);
                ImGui.Checkbox("Parameters visible", ref ParameterWindowVisible);

                var io = ImGui.GetIO();
                ImGui.Text(
                    (io.KeyAlt ? "Alt" : "")
                    + (io.KeyCtrl ? "Ctrl" : "")
                    + (io.KeyShift ? "Shift" : ""));

                var sb = new StringBuilder();
                for (var i = 0; i < ImGui.GetIO().KeysDown.Count; i++)
                {
                    if (io.KeysDown[i])
                    {
                        Key k = (Key)i;

                        sb.Append($"{k} [{i}]");
                    }
                }
                ImGui.Text("Pressed keys:" + sb);

                if (ImGui.Button("Open new Graph Canvas"))
                    OpenNewGraphWindow();

                if (ImGui.Button("New Parameter View"))
                    OpenNewParameterView();

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
                    T3Style.Draw();

            }
            ImGui.End();

        }
    }

}