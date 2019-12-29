using ImGuiNET;
using SharpDX;

namespace T3.Gui.InputUi.SingleControl
{
    public class Size2InputUi : SingleControlInputUi<Size2>
    {
        protected override bool DrawSingleEditControl(string name, ref Size2 value)
        {
            return ImGui.DragInt2("##int2Edit", ref value.Width);
        }

        protected override void DrawReadOnlyControl(string name, ref Size2 value)
        {
            DrawEditControl(name, ref value);
        }
    }
}