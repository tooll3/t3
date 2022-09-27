using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_763e1851_36ef_4443_92d9_4d49ee479357
{
    public class BoundingSphere : Instance<BoundingSphere>
    {
        [Output(Guid = "2b0ac199-06b2-4183-a0b4-b650dafe10b4")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "7d44435c-a02e-446e-ada9-1f0d4c432fdd")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "664ebf91-fccc-4531-ad64-16bcddf9a71b")]
        public readonly InputSlot<float> Radius = new InputSlot<float>();


    }
}

