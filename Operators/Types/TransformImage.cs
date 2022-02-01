using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_32e18957_3812_4f64_8663_18454518d005
{
    public class TransformImage : Instance<TransformImage>
    {
        [Output(Guid = "54831ac3-d747-4cdf-9520-3cfd651158bf")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "3aab9b12-1e02-4d7a-83b6-da1500a6bcbf")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "6f4184f1-6017-4bcc-ac1f-5ea4862bfb0c")]
        public readonly InputSlot<System.Numerics.Vector2> Offset = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "53538db0-2b65-4c92-80b1-ea6aecbc49ae")]
        public readonly InputSlot<System.Numerics.Vector2> Stretch = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "5b8ff5d7-e4d6-4631-8f0a-afb8086383e7")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

        [Input(Guid = "6a786aa9-edf4-4363-9e34-0ddc7e763f0b")]
        public readonly InputSlot<float> Rotation = new InputSlot<float>();

        [Input(Guid = "5c76dc8d-3a28-4b93-b3a0-e008c1ff14e9")]
        public readonly InputSlot<SharpDX.Size2> Resolution = new InputSlot<SharpDX.Size2>();

        [Input(Guid = "b3edcd1e-e0ce-43a7-98e9-1568e2329ed5")]
        public readonly InputSlot<bool> Mirror = new InputSlot<bool>();

        [Input(Guid = "4d8073e1-720d-4cac-bc4c-00be40c8687e")]
        public readonly InputSlot<SharpDX.Direct3D11.TextureAddressMode> WrapMode = new InputSlot<SharpDX.Direct3D11.TextureAddressMode>();
    }
}

