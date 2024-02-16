using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.point
{
	[Guid("e555a0ca-8c81-4436-9f77-8c7a327d7379")]
    public class RandomizePointsExample : Instance<RandomizePointsExample>
    {
        [Output(Guid = "d078d68a-e19e-48c9-912d-47e2cb906b42")]
        public readonly Slot<Texture2D> ImgOutput = new Slot<Texture2D>();

        [Input(Guid = "96c77fb4-7137-4cd7-9285-3ea848c18255")]
        public readonly InputSlot<float> Float = new InputSlot<float>();
        
    }
}

