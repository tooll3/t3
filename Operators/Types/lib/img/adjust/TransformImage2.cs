using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_a2ecf256_ac9f_4f99_92af_7ae749a3e3d9
{
    public class TransformImage2 : Instance<TransformImage2>
    {
        [Output(Guid = "26cc7d34-b1fa-4d38-b1bc-d2046ba10170")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "ad19cb79-9f94-432e-9692-a57017c45c84")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "ea303055-c024-4356-ae91-0241fcb9f18c")]
        public readonly InputSlot<System.Numerics.Vector2> Offset = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "d511e94f-189d-4f69-a43f-3af9dbf00808")]
        public readonly InputSlot<System.Numerics.Vector2> Stretch = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "be4f80a3-95ba-47a5-adf7-0b79034b1841")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

        [Input(Guid = "1f028ccb-1436-4cb8-a1d7-ec7a03bc05f8")]
        public readonly InputSlot<float> Rotation = new InputSlot<float>();

        [Input(Guid = "26d8e557-e3b5-4cbc-a00e-785452e9b639")]
        public readonly InputSlot<SharpDX.Size2> Resolution = new InputSlot<SharpDX.Size2>();

        [Input(Guid = "fc03f1d7-5e46-4b77-be90-17ff99fb1d51")]
        public readonly InputSlot<bool> Mirror = new InputSlot<bool>();

        [Input(Guid = "5c99a662-c657-4188-a3b6-53442d62a5da")]
        public readonly InputSlot<bool> GenerateMips = new InputSlot<bool>();

        [Input(Guid = "65a799bb-a6ab-46dc-9e4e-269c2602d2f2")]
        public readonly InputSlot<SharpDX.Direct3D11.Filter> Filter = new InputSlot<SharpDX.Direct3D11.Filter>();

        [Input(Guid = "4127e547-9c6f-4aee-aefb-e1d6cd472b7f")]
        public readonly InputSlot<SharpDX.Direct3D11.TextureAddressMode> WrapMode = new InputSlot<SharpDX.Direct3D11.TextureAddressMode>();
    }
}

