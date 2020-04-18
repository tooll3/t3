using ImGuiNET;
using SharpDX;

namespace T3.Gui.InputUi.SingleControl
{
    public class Int3InputUi : SingleControlInputUi<Int3>
    {
        public override IInputUi Clone()
        {
            return new Int3InputUi()
                   {
                       InputDefinition = InputDefinition,
                       Parent = Parent,
                       PosOnCanvas = PosOnCanvas,
                       Relevancy = Relevancy
                   };
        }

        protected override bool DrawSingleEditControl(string name, ref Int3 value)
        {
            return ImGui.DragInt3("##int3Edit", ref value.X);
        }

        protected override void DrawReadOnlyControl(string name, ref Int3 value)
        {
            DrawEditControl(name, ref value, false);
        }
    }
}