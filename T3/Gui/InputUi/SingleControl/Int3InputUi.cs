using ImGuiNET;
using SharpDX;

namespace T3.Gui.InputUi.SingleControl
{
    public class Int3InputUi : SingleControlInputUi<Int3>
    {
        protected override bool DrawSingleEditControl(string name, ref Int3 value)
        {
            return ImGui.DragInt3("##int3Edit", ref value.X);
        }

        protected override void DrawReadOnlyControl(string name, ref Int3 value)
        {
            DrawEditControl(name, ref value);
        }
    }
}