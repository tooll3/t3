using SharpDX.Direct3D11;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_c6014c28_c6ab_4b4e_b6bf_0cee92fb4b40
{
    public class ConvertEquirectangle : Instance<ConvertEquirectangle>
    {
        [Output(Guid = "000b79eb-b390-4b6b-9fdc-b99f12bc308d")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


        [Input(Guid = "57ce1074-7971-4542-95c8-86f58ed75c7d")]
        public readonly InputSlot<Texture2D> Image = new InputSlot<Texture2D>();

        [Input(Guid = "07d45e2f-75dd-455c-b8fe-b96ab2f830a2")]
        public readonly InputSlot<Int2> Resolution = new InputSlot<Int2>();

    }
}

