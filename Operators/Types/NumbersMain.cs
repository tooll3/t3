using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_dba25675_0632_40b7_a807_03372e334cc9
{
    public class NumbersMain : Instance<NumbersMain>
    {
        [Output(Guid = "54bb1076-df85-4ab1-8f41-6014127b8450")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();

        [Output(Guid = "4a22b27d-43ec-43ca-a3cc-cc48720d8339")]
        public readonly Slot<Texture2D> TextureOutput2 = new Slot<Texture2D>();


    }
}

