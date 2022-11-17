using T3.Core;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_e4e608e7_1248_4d73_910c_e8bfbb34251b
{
    public class SimpleLiquid : Instance<SimpleLiquid>
    {
        [Output(Guid = "9397f264-fde0-4806-b287-5cac9fd638b1")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();

        [Input(Guid = "238817b3-2025-4b15-8ff6-0a779f349012")]
        public readonly MultiInputSlot<T3.Core.DataTypes.Command> Command = new MultiInputSlot<T3.Core.DataTypes.Command>();

        [Input(Guid = "ccfc2670-c272-4641-9a53-69317b7b7acb")]
        public readonly InputSlot<float> Shade = new InputSlot<float>();

    }
}

