using ImGuiNET;
using System.Numerics;

namespace T3.Gui.InputUi
{
    public class Vector4InputUi : SingleControlInputUi<Vector4>
    {
        public override bool DrawSingleEditControl(string name, ref Vector4 value)
        {
            return ImGui.ColorEdit4("##Vector4Edit", ref value, ImGuiColorEditFlags.Float);
        }

        protected override void DrawValueDisplay(string name, ref Vector4 value)
        {
            DrawEditControl(name, ref value);
        }
    }
}