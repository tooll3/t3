using System.Numerics;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9123651a_5df8_4f85_9e14_2068f33e2ff1
{
    public class DrawBoxGizmo : Instance<DrawBoxGizmo>
    {
        [Output(Guid = "9e1e233f-bd4a-461b-983d-90a4d88ef286")]
        public readonly Slot<Command> Output = new();


        [Input(Guid = "656697b8-b271-463b-9e38-fdb5758d3736")]
        public readonly InputSlot<Vector4> Color = new();

        [Input(Guid = "6f95e60a-f259-45fa-b23f-ce284cc9275e")]
        public readonly InputSlot<System.Numerics.Vector3> Stretch = new();

        [Input(Guid = "A331BFBB-8876-4E27-94B3-782E64EFD72A")]
        public readonly InputSlot<float> Scale = new ();
        
        [Input(Guid = "83bb304e-3ed3-405f-92c7-58d263d9aafc")]
        public readonly InputSlot<System.Numerics.Vector3> Position = new();

    }
}

