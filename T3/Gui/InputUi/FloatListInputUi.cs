using System.Collections.Generic;
using ImGuiNET;

namespace T3.Gui.InputUi
{
    public class FloatListInputUi : SingleControlInputUi<List<float>>
    {
        protected override bool DrawSingleEditControl(string name, ref List<float> list)
        {
            var outputString = string.Join(", ", list);
            ImGui.Text($"{outputString}");
            return false;
        }

        protected override void DrawValueDisplay(string name, ref List<float> list)
        {
            var outputString = string.Join(", ", list);
            ImGui.Text($"{outputString}");
        }
    }
}