using System.Diagnostics;
using ImGuiNET;
using SharpDX;
using SharpDX.Direct3D11;
using T3.Core.Operator;

namespace T3.Gui.OutputUi
{
    public class ValueOutputUi<T> : OutputUi<T>
    {
        public override void DrawValue(ISlot slot)
        {
            if (slot is Slot<T> typedSlot)
            {
                Invalidate(slot);
                _evaluationContext.Reset();
                var value = typedSlot.GetValue(_evaluationContext);
                ImGui.Text($"{value}");
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }

    public class FloatOutputUi : ValueOutputUi<float>
    {
    }
   
    public class IntOutputUi : ValueOutputUi<int>
    {
    }

    public class StringOutputUi : ValueOutputUi<string>
    {
    }

    public class Size2OutputUi : ValueOutputUi<Size2>
    {
    }

    public class ComputeShaderOutputUi : ValueOutputUi<ComputeShader>
    {
    }

    public class BufferOutputUi : ValueOutputUi<SharpDX.Direct3D11.Buffer>
    {
    }

    public class SamplerStateOutputUi : ValueOutputUi<SamplerState>
    {
    }
}