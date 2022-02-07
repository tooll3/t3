using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Operators.Types.Id_fd9bffd3_5c57_462f_8761_85f94c5a629b;

namespace T3.Operators.Types.Id_95558338_81a5_4ecc_9d5c_1c6fb5f6f4fa
{
    public class DrawSprites : Instance<DrawSprites>
    {
        [Output(Guid = "d2d834bd-00f4-46a0-ad20-3f399e107229")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "f25285d7-e23c-499a-8711-76cdbab79cea")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "333c119c-79d0-410c-a20a-5d035a7192ee")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "12fb90d8-c1e7-4860-bda4-801d22350b3c")]
        public readonly InputSlot<System.Numerics.Vector2> Stretch = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "1a4d523d-48e3-4c86-af1a-3476c5dff0cc")]
        public readonly InputSlot<System.Numerics.Vector2> Offset = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "ce8b4cc8-95bd-429d-a170-8ba7edeafea6")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "6e230b1f-3b6b-46fa-9469-1c85196c8ffd")]
        public readonly InputSlot<float> UseWForSize = new InputSlot<float>();

        [Input(Guid = "a7f7f2b7-74cd-42c5-96b7-3bf2438f2803")]
        public readonly InputSlot<float> Rotate = new InputSlot<float>();

        [Input(Guid = "0e4d1e87-87a4-46d3-908c-b3a1ca15ec2e")]
        public readonly InputSlot<System.Numerics.Vector3> RotateAxis = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "5e71cd7f-ef66-4d83-b4e2-4018dc134a01")]
        public readonly InputSlot<SharpDX.Size2> TextureCells = new InputSlot<SharpDX.Size2>();

        [Input(Guid = "3ca52af7-5710-4912-a171-11fffd91fd1a")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture_ = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "257c4149-7c27-48ce-abd5-c0dbfc1631b3")]
        public readonly InputSlot<int> BlendMod = new InputSlot<int>();

        [Input(Guid = "fdd797b2-f02a-4f92-a97f-c2c361758c48")]
        public readonly InputSlot<bool> EnableDepthWrite = new InputSlot<bool>();
    }
}

