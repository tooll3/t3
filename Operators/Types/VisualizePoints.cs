using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_37bdbafc_d14c_4b81_91c3_8f63c3b63812
{
    public class VisualizePoints : Instance<VisualizePoints>
    {
        [Output(Guid = "b0294b73-58a9-4d79-b3e2-caaed304109d", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "54fc4cd7-dfc3-4690-9fd1-2b555f7656d4")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "621bf2cf-8d49-4b5f-88b9-4460045e8914")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "C85649DF-A235-49D6-A964-C69B299FB4B5")]
        public readonly InputSlot<T3.Core.Operator.GizmoVisibility> Visibility = new InputSlot<T3.Core.Operator.GizmoVisibility>();

        [Input(Guid = "40a04de8-54aa-4f66-acea-80ffc4dab7bd")]
        public readonly InputSlot<float> PointSize = new InputSlot<float>();

    }
}

