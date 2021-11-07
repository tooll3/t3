using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_00501ac1_b638_4ec4_a17d_ece4f6f2314c
{
    public class PictureShow : Instance<PictureShow>
    {
        [Output(Guid = "d32cdb2f-2700-4352-ac1b-90e593b024db")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();


        [Input(Guid = "8c2393e5-5ca0-4420-ab64-efe34aa3683f")]
        public readonly InputSlot<float> Float = new InputSlot<float>();

        [Input(Guid = "118ba9e8-2b78-4978-8c36-be8487066505")]
        public readonly InputSlot<float> Exposure = new InputSlot<float>();

        [Input(Guid = "ea93e85c-9f3f-4796-9473-b3ca7f5826b1")]
        public readonly InputSlot<Texture2D> Texture2d = new InputSlot<Texture2D>();

    }
}

