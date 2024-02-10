using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9d71d46c_f3d8_4bf4_a104_38c0b37cc88b
{
    public class Equirectangle : Instance<Equirectangle>
    {

        [Output(Guid = "52dacae9-3407-4748-adb3-dc691178e9bc")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> OutputColor = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "2097d7c0-604b-4909-8cf1-ca9793dc53ec")]
        public readonly InputSlot<T3.Core.DataTypes.Command> InputCommand = new InputSlot<T3.Core.DataTypes.Command>();

        [Input(Guid = "9e35ef34-0b02-40f6-93d5-5163346d681a")]
        public readonly InputSlot<int> Dimension = new InputSlot<int>();

        [Output(Guid = "bbc85c64-3d0a-47ce-8126-9f90d4b60fac")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> OutputDepth = new Slot<SharpDX.Direct3D11.Texture2D>();


    }
}

