using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_82ad8911_c930_4851_803d_3f24422445bc
{
    public class RadialGradient : Instance<RadialGradient>
    {
        [Output(Guid = "9785937a-2b8f-4b2e-92ac-98ec067a40f2")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "54bca43c-fc2b-4a40-b991-8b76e35eee01")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "1cf83367-7a34-4369-86d8-77dd4fe48d63")]
        public readonly InputSlot<System.Numerics.Vector2> Center = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "bfdcfed4-263f-4115-a1a8-291088e34c0a")]
        public readonly InputSlot<float> Width = new InputSlot<float>();

        [Input(Guid = "5cd13f08-2e72-41c7-82a6-b58726d57acc")]
        public readonly InputSlot<float> Rotation = new InputSlot<float>();

        [Input(Guid = "6c1dc695-1c0a-47fe-aea1-e3abec904883")]
        public readonly InputSlot<bool> PingPong = new InputSlot<bool>();

        [Input(Guid = "eab31c38-0e6f-432a-9f15-04bfb0aae28c")]
        public readonly InputSlot<bool> Repeat = new InputSlot<bool>();

        [Input(Guid = "cf2e1698-f996-4b83-8b59-3150e75d59c6")]
        public readonly InputSlot<SharpDX.Size2> Resolution = new InputSlot<SharpDX.Size2>();

        [Input(Guid = "945d14de-62aa-47cf-81e3-a91c52811d8e")]
        public readonly InputSlot<float> Bias = new InputSlot<float>();

        [Input(Guid = "3f5a284b-e2f0-47e2-bf79-2a7fe8949519")]
        public readonly InputSlot<T3.Core.DataTypes.Gradient> Gradient = new InputSlot<T3.Core.DataTypes.Gradient>();
    }
}

