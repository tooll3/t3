using ImGuiNET;

namespace T3.Gui.InputUi
{
    public class IntInputUi : SingleControlInputUi<int>
    {
        public override bool DrawSingleEditControl(string name, ref int value)
        {
            return ImGui.DragInt("##intParam", ref value);
        }

        protected override void DrawValueDisplay(string name, ref int value)
        {
            ImGui.InputInt(name, ref value, 0, 0, ImGuiInputTextFlags.ReadOnly);
        }
    }
}