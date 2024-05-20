using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_2a5475c8_9e16_409f_8c40_a3063e045d38
{
    public class DetectEdges : Instance<DetectEdges>
    {
        [Output(Guid = "caf8af48-8819-49b4-890b-89545c8c0ff5")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new();


        [Input(Guid = "4041b6d8-15e5-428c-9967-7105975a46f7")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new();

        [Input(Guid = "7f66aa8d-fbdd-47d6-ba38-07e257e19401")]
        public readonly InputSlot<float> SampleRadius = new();

        [Input(Guid = "d3197979-b418-4182-b1c9-f3126b175f8d")]
        public readonly InputSlot<float> Strength = new();

        [Input(Guid = "9dae724d-7be8-4f82-8907-28550ddbf6e6")]
        public readonly InputSlot<float> Contrast = new();

        [Input(Guid = "6d10c73c-37b8-443b-94d9-854b04027a3c")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new();

        [Input(Guid = "c0a17636-f75b-45c0-ab63-cb0f9130a7ac")]
        public readonly InputSlot<float> MixOriginal = new();

        [Input(Guid = "921b8a04-d3b5-408e-ad3e-311a4c9890b1")]
        public readonly InputSlot<bool> OutputAsTransparent = new();
    }
}

