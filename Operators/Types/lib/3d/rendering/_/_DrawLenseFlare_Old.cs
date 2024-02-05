using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_95558338_81a5_4ecc_9d5c_1c6fb5f6f4fa
{
    public class _DrawLenseFlare_Old : Instance<_DrawLenseFlare_Old>
    {
        [Output(Guid = "d2d834bd-00f4-46a0-ad20-3f399e107229")]
        public readonly Slot<Command> Output = new();

        [Input(Guid = "f25285d7-e23c-499a-8711-76cdbab79cea")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> SpriteBuffer = new();

        [Input(Guid = "333c119c-79d0-410c-a20a-5d035a7192ee")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new();

        [Input(Guid = "ce8b4cc8-95bd-429d-a170-8ba7edeafea6")]
        public readonly InputSlot<float> Size = new();

        [Input(Guid = "3ca52af7-5710-4912-a171-11fffd91fd1a")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture_ = new();

        [Input(Guid = "257c4149-7c27-48ce-abd5-c0dbfc1631b3")]
        public readonly InputSlot<int> BlendMod = new();

        [Input(Guid = "fdd797b2-f02a-4f92-a97f-c2c361758c48")]
        public readonly InputSlot<bool> EnableDepthWrite = new();
    }
}

