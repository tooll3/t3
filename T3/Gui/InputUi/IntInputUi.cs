using System.Numerics;
using ImGuiNET;
using T3.Gui.Interaction;

namespace T3.Gui.InputUi
{
    public class IntInputUi : SingleControlInputUi<int>
    {
        public override bool DrawSingleEditControl(string name, ref int value)
        {
            var result= SingleValueEdit.Draw(ref value, new Vector2(-1, 0));
            return result == InputEditState.Modified;
        }

        protected override void DrawValueDisplay(string name, ref int value)
        {
            ImGui.InputInt(name, ref value, 0, 0, ImGuiInputTextFlags.ReadOnly);
        }
    }
}