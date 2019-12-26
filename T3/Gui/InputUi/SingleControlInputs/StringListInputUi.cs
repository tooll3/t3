using System.Collections.Generic;
using ImGuiNET;

namespace T3.Gui.InputUi.SingleControlInputs
{
    public class StringListInputUi : SingleControlInputUi<List<string>>
    {
        protected override bool DrawSingleEditControl(string name, ref List<string> list)
        {
            var outputString = string.Join(", ", list);
            ImGui.Text($"{outputString}");
            return false;
        }

        protected override void DrawValueDisplay(string name, ref List<string> list)
        {
            var outputString = string.Join(", ", list);
            ImGui.Text($"{outputString}");
        }
    }
}