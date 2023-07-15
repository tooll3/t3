using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_37bdbafc_d14c_4b81_91c3_8f63c3b63812
{
    public class VisualizePoints : Instance<VisualizePoints>
    {
        [Output(Guid = "b0294b73-58a9-4d79-b3e2-caaed304109d", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "54fc4cd7-dfc3-4690-9fd1-2b555f7656d4")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "8f72275d-d903-4372-852c-51c3db35fe90")]
        public readonly InputSlot<bool> ShowCenterPoints = new InputSlot<bool>();

        [Input(Guid = "d0ac63c5-639b-4b3c-b40b-348b76fa0fd2")]
        public readonly InputSlot<bool> ShowAxis = new InputSlot<bool>();

        [Input(Guid = "621bf2cf-8d49-4b5f-88b9-4460045e8914")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "b857b40b-2ca7-42a4-bebe-1cb11700ed71")]
        public readonly InputSlot<float> LineThickness = new InputSlot<float>();

        [Input(Guid = "90173b57-cd09-4270-a16e-6e2454882b9b")]
        public readonly InputSlot<int> StartIndex = new InputSlot<int>();

        [Input(Guid = "c4332cb5-4dbc-4dd1-a738-cee8a3098c17")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "40a04de8-54aa-4f66-acea-80ffc4dab7bd")]
        public readonly InputSlot<float> PointSize = new InputSlot<float>();

        [Input(Guid = "C85649DF-A235-49D6-A964-C69B299FB4B5")]
        public readonly InputSlot<T3.Core.Operator.GizmoVisibility> Visibility = new InputSlot<T3.Core.Operator.GizmoVisibility>();

        [Input(Guid = "d2472768-dd40-436f-af1b-7359289b5118")]
        public readonly InputSlot<bool> ShowIndices = new InputSlot<bool>();

        [Input(Guid = "bbc26907-416d-4168-9e89-72ee1c6a530e")]
        public readonly InputSlot<bool> ShowAttributeList = new InputSlot<bool>();

        [Input(Guid = "98fe7249-39ea-4f45-b045-36e07a8f2018")]
        public readonly InputSlot<bool> ShowVelocity = new InputSlot<bool>();

        [Input(Guid = "08174efd-78e5-4552-b559-5aa7b1b8c33e")]
        public readonly InputSlot<bool> ShowSpritePlane = new InputSlot<bool>();

        [Input(Guid = "7b2054d4-e6b5-43e3-9cbe-1d2073ae35aa")]
        public readonly InputSlot<bool> ShowXyRadius = new InputSlot<bool>();

    }
}

