using ImGuiNET;

namespace T3.Gui.InputUi
{
    public class BoolInputUi : SingleControlInputUi<bool>
    {
        protected override bool DrawSingleEditControl(string name, ref bool value)
        {
            return ImGui.Checkbox("##boolParam", ref value);
        }

        protected override void DrawValueDisplay(string name, ref bool value)
        {
            ImGui.Text(value.ToString());
        }
    }
}