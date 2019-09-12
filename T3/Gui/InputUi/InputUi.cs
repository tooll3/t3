using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Gui.InputUi
{
    public class Texture2dInputUi : SingleControlInputUi<Texture2D>
    {
        public override bool DrawSingleEditControl(string name, ref Texture2D value)
        {
            return false;//ImGui.DragInt2("##int2Edit", ref value.Width);
        }

        protected override void DrawValueDisplay(string name, ref Texture2D value)
        {
//            DrawEditControl(name, ref value);
        }
    }

    public class ComputeShaderInputUi : SingleControlInputUi<ComputeShader>
    {
        public override bool DrawSingleEditControl(string name, ref ComputeShader value)
        {
            return false;//ImGui.DragInt2("##int2Edit", ref value.Width);
        }

        protected override void DrawValueDisplay(string name, ref ComputeShader value)
        {
//            DrawEditControl(name, ref value);
        }
    }

    public class BufferInputUi : SingleControlInputUi<Buffer>
    {
        public override bool DrawSingleEditControl(string name, ref Buffer value)
        {
            return false;//ImGui.DragInt2("##int2Edit", ref value.Width);
        }

        protected override void DrawValueDisplay(string name, ref Buffer value)
        {
//            DrawEditControl(name, ref value);
        }
    }

    public class SamplerStateInputUi : SingleControlInputUi<SamplerState>
    {
        public override bool DrawSingleEditControl(string name, ref SamplerState value)
        {
            return false;//ImGui.DragInt2("##int2Edit", ref value.Width);
        }

        protected override void DrawValueDisplay(string name, ref SamplerState value)
        {
//            DrawEditControl(name, ref value);
        }
    }
}