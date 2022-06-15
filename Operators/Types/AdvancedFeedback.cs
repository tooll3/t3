using T3.Core;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_33424f7f_ea2d_4753_bbc3_8df00830c4b5
{
    public class AdvancedFeedback : Instance<AdvancedFeedback>
    {
        [Output(Guid = "a977681b-2f7b-44de-a29e-3ba00e2260b0")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();

        [Input(Guid = "0f29ca23-b5fb-484e-8ae6-8ed70d67d623")]
        public readonly MultiInputSlot<T3.Core.Command> Command = new MultiInputSlot<T3.Core.Command>();

        [Input(Guid = "1d5207f0-132e-42e9-9b2b-171d092d6cac")]
        public readonly InputSlot<float> Displacement = new InputSlot<float>();

        [Input(Guid = "3ae3bc6a-5950-4f8b-b2c1-5247dfb9221c")]
        public readonly InputSlot<float> DisplaceOffset = new InputSlot<float>();

        [Input(Guid = "882acab3-7cb9-42e8-8f63-5382c83422c2")]
        public readonly InputSlot<float> SampleDistance = new InputSlot<float>();

        [Input(Guid = "95b99630-6d27-41d6-9e02-a3e905d023d7")]
        public readonly InputSlot<float> Shade = new InputSlot<float>();

        [Input(Guid = "f1a53a46-fa5c-49af-9d53-0d68cfe1b33e")]
        public readonly InputSlot<float> BlurRadius = new InputSlot<float>();

        [Input(Guid = "fc1b2bc8-6756-4e78-a90b-8af691d85875")]
        public readonly InputSlot<float> Twist = new InputSlot<float>();

        [Input(Guid = "d2b9ef03-1641-4949-86ff-b71dc1fb3ad0")]
        public readonly InputSlot<float> Zoom = new InputSlot<float>();

        [Input(Guid = "849540c3-7ffd-40a7-a78b-3d051256e5f1")]
        public readonly InputSlot<float> Rotate = new InputSlot<float>();

        [Input(Guid = "482fba6b-f92e-418c-a8f4-8da0f546c4a6")]
        public readonly InputSlot<System.Numerics.Vector2> Offset = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "c3827e2e-2d15-475c-9b2f-03a861e97fc5")]
        public readonly InputSlot<bool> IsEnabled = new InputSlot<bool>();

    }
}

