using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1a411be2_1757_4019_8ce2_e29f808ed839
{
    public class CheckerBoard : Instance<CheckerBoard>
    {
        [Output(Guid = "9dd9dbeb-b506-4d10-97b7-34feaab91f07")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new();

        [Input(Guid = "1192cfbe-585f-45f0-a37b-5fe78ca32d7b")]
        public readonly InputSlot<System.Numerics.Vector4> ColorA = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "b08aba90-f33f-4402-bb7b-bcfc4bb624ce")]
        public readonly InputSlot<System.Numerics.Vector4> ColorB = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "70c4ecc7-72a2-42ee-8546-cbff2c08aa27")]
        public readonly InputSlot<System.Numerics.Vector2> Stretch = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "e367bd72-c37c-4a7f-8441-698161ba75f8")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

        [Input(Guid = "48d5cd1d-3ca1-4075-a55e-21cec39b4525")]
        public readonly InputSlot<bool> UseAspectRatio = new InputSlot<bool>();

        [Input(Guid = "cb36f66b-99d7-4468-adbf-86e168a76ade")]
        public readonly InputSlot<System.Numerics.Vector2> Offset = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "63ebb6c9-e8a5-43e4-97a4-3a34ad585474")]
        public readonly InputSlot<T3.Core.DataTypes.Vector.Int2> Resolution = new InputSlot<T3.Core.DataTypes.Vector.Int2>();
    }
}

