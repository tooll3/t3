using ImGuiNET;
using SharpDX;

namespace T3.Gui.InputUi
{
    public class Size2InputUi : SingleControlInputUi<Size2>
    {
        public override bool DrawSingleEditControl(string name, ref Size2 value)
        {
            return ImGui.DragInt2("##int2Edit", ref value.Width);
        }

        protected override void DrawValueDisplay(string name, ref Size2 value)
        {
            DrawEditControl(name, ref value);
        }
    }
}