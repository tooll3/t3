using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_c8590f8f_cca1_434a_a880_67bb91920e1a
{
    public class Blob : Instance<Blob>
    {
        [Output(Guid = "1d2a7948-4c89-407a-a98f-9810f60ef75e")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();


        [Input(Guid = "630d4d0b-d4ca-4987-93ca-7eb782ebccc6")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "bbda1c8c-fa81-43ef-b773-f7ecfb8968e1")]
        public readonly InputSlot<System.Numerics.Vector4> Fill = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "de54f18b-6a1e-4610-8d6d-58897df6959b")]
        public readonly InputSlot<System.Numerics.Vector4> Background = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "8a908810-2482-4088-8b21-a7ee15531e64")]
        public readonly InputSlot<System.Numerics.Vector2> Size = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "325461c8-2892-4e8d-8d3b-0eea1bcc03f9")]
        public readonly InputSlot<System.Numerics.Vector2> Position = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "41f4065a-0b65-4dda-9cd3-ef7802ed170b")]
        public readonly InputSlot<float> Round = new InputSlot<float>();

        [Input(Guid = "8846aa50-e4d0-433c-9e5b-013a93f17f79")]
        public readonly InputSlot<float> Feather = new InputSlot<float>();

        [Input(Guid = "2d3a7e9a-9efe-486a-8c33-10a5a16dc17b")]
        public readonly InputSlot<float> FeatherBias = new InputSlot<float>();

        [Input(Guid = "9786d2b3-2cff-40fb-8ab7-d96f3ac3dd76")]
        public readonly InputSlot<SharpDX.Size2> Resolution = new InputSlot<SharpDX.Size2>();

        [Input(Guid = "8232e748-5fc2-488d-8559-ac9ff621f95d")]
        public readonly InputSlot<float> Rotate = new InputSlot<float>();
    }
}

