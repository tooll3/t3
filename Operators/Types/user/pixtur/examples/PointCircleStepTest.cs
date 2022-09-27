using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_2b71c65c_3abd_4971_8491_ab398affbe81
{
    public class PointCircleStepTest : Instance<PointCircleStepTest>
    {
        [Output(Guid = "efe5cc38-6f72-4105-a17b-a651cea3c80a")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();

        [Input(Guid = "cb5aac10-96c5-4af2-9d66-eaddfe7c349b")]
        public readonly InputSlot<System.Numerics.Vector4> Fill = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "9407eab0-f3a0-486b-862f-ba8b4f43862c")]
        public readonly InputSlot<string> InputString = new InputSlot<string>();

        [Input(Guid = "6409ef7b-c9b2-4e67-bca1-d5b76fdd8858")]
        public readonly InputSlot<float> Size = new InputSlot<float>();


    }
}

