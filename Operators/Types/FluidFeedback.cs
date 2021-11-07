using T3.Core;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f9d453d1_04d9_43ef_9189_50008f93bcc2
{
    public class FluidFeedback : Instance<FluidFeedback>
    {
        [Output(Guid = "b9baba42-18b6-4792-929d-bf628ce8a488")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();

        [Input(Guid = "ebd35d33-dc73-46ae-a82c-2060d750018a")]
        public readonly MultiInputSlot<T3.Core.Command> Command = new MultiInputSlot<T3.Core.Command>();

        [Input(Guid = "a7669abe-65c7-4745-97a6-d0d80f6a3150")]
        public readonly InputSlot<float> Displacement = new InputSlot<float>();

        [Input(Guid = "78b314b8-f9d2-4723-9b11-c07ba926db86")]
        public readonly InputSlot<float> Shade = new InputSlot<float>();

        [Input(Guid = "8060756f-72a4-490b-9677-872b70e73b3a")]
        public readonly InputSlot<float> BlurRadius = new InputSlot<float>();

        [Input(Guid = "297a2220-0648-47d3-82a1-c1077a1326a4")]
        public readonly InputSlot<float> Twist = new InputSlot<float>();

        [Input(Guid = "98662eab-90b9-4af1-8ff3-ba6709b5038e")]
        public readonly InputSlot<float> Zoom = new InputSlot<float>();

        [Input(Guid = "859977e7-afaf-44b5-956a-d42da972330c")]
        public readonly InputSlot<float> Rotate = new InputSlot<float>();

        [Input(Guid = "41c080de-575f-4041-b605-8f55e4bcf797")]
        public readonly InputSlot<float> SampleDistance = new InputSlot<float>();

        [Input(Guid = "4109af01-5c9a-4a9f-af7f-87fbcdcece3d")]
        public readonly InputSlot<System.Numerics.Vector2> Offset = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "806221f8-6e31-45ec-b62e-5baac6c1fd54")]
        public readonly InputSlot<float> DisplaceOffset = new InputSlot<float>();

    }
}

