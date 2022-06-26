using System.Numerics;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9123651a_5df8_4f85_9e14_2068f33e2ff1
{
    public class BoundingBox : Instance<BoundingBox>
    {
        [Output(Guid = "9e1e233f-bd4a-461b-983d-90a4d88ef286", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new Slot<Command>();


        [Input(Guid = "656697b8-b271-463b-9e38-fdb5758d3736")]
        public readonly InputSlot<Vector4> Value = new InputSlot<Vector4>();

        [Input(Guid = "6f95e60a-f259-45fa-b23f-ce284cc9275e")]
        public readonly InputSlot<System.Numerics.Vector3> Size = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "83bb304e-3ed3-405f-92c7-58d263d9aafc")]
        public readonly InputSlot<System.Numerics.Vector3> Position = new InputSlot<System.Numerics.Vector3>();

    }
}

