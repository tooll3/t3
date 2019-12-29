using System.Collections.Generic;
using ImGuiNET;

namespace T3.Gui.InputUi.SingleControl
{
    public class FloatListInputUi : SingleControlInputUi<List<float>>
    {
        protected override bool DrawSingleEditControl(string name, ref List<float> list)
        {
            var outputString = string.Join(", ", list);
            ImGui.Text($"{outputString}");
            return false;
        }

        protected override void DrawReadOnlyControl(string name, ref List<float> list)
        {
            var outputString = string.Join(", ", list);
            ImGui.Text($"{outputString}");
        }
    }
}