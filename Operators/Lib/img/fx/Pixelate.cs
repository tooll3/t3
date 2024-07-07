using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1fa725a1_dab6_4a2a_8a4d_6efdfba5cf05
{
    public class Pixelate : Instance<Pixelate>
    {
        [Output(Guid = "47e693ca-a695-4f95-9f02-9b76894ee91c")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new();

        [Input(Guid = "7db8987c-f128-476f-93bb-d79e761caecc")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "25711dcb-4b53-4a00-8b9d-7b653f8eaf59")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "047db178-dab3-4123-90ec-becb4f439f4e")]
        public readonly InputSlot<int> Divisor = new InputSlot<int>();

        [Input(Guid = "824bd327-4c52-422b-bd83-c568db8c0ea9")]
        public readonly InputSlot<T3.Core.DataTypes.Vector.Int2> TileAmount = new InputSlot<T3.Core.DataTypes.Vector.Int2>();

        [Input(Guid = "f8a8381b-3a8c-4468-b66b-0a5be6f040bd")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Shape = new InputSlot<SharpDX.Direct3D11.Texture2D>();
    }
}

