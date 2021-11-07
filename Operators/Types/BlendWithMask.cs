using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_7da55d23_0bd1_457b_a036_9b6b51d2e34b
{
    public class BlendWithMask : Instance<BlendWithMask>
    {
        [Output(Guid = "dcf13066-95dc-4c0f-8c8c-379f396502ce")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "7d878133-43cf-44a3-87a2-18d44f74f17d")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> ImageA = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "0f542667-8b2c-4fd7-9f9a-d63f1573d25a")]
        public readonly InputSlot<System.Numerics.Vector4> ColorA = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "c68c887c-16f1-4fa2-89a4-4a07d44a0df6")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> ImageB = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "f551d82e-bbd5-40fd-9d84-e08d97f06c46")]
        public readonly InputSlot<System.Numerics.Vector4> ColorB = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "d08813be-bd43-4229-86b7-4e53b62b8561")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Mask = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "ff0e3f81-1340-40e7-9c95-b88938d63901")]
        public readonly InputSlot<SharpDX.Size2> Resolution = new InputSlot<SharpDX.Size2>();

    }
}

