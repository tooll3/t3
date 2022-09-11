using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_723cd13e_0ca0_4995_a80b_a3b616400997
{
    public class _TransitionEffect : Instance<_TransitionEffect>
    {
        [Output(Guid = "be5a4e9f-8c9d-4838-82b1-b826ac183640")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();


        [Input(Guid = "7e75adf0-200e-4f15-b333-88105e0994d8")]
        public readonly InputSlot<Texture2D> ImageA = new InputSlot<Texture2D>();

    }
}

