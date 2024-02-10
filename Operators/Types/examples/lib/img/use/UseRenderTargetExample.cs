using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1fdd634f_4c6a_4615_b75a_0c46732c9826
{
    public class UseRenderTargetExample : Instance<UseRenderTargetExample>
    {
        [Output(Guid = "2f32cf47-be6e-4ac8-a2e5-6e967edb64b1")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

