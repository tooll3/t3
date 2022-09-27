using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_2d4aee4a_4eb7_41c5_95de_1a3929c24c3c
{
    public class BloomExample : Instance<BloomExample>
    {
        [Output(Guid = "c5ee716c-4478-4183-bbb3-919a2648b1ad")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();


    }
}

