using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.point
{
	[Guid("25d26231-730e-4376-b256-e34eca6290ce")]
    public class GetPointDataFromListExample : Instance<GetPointDataFromListExample>
    {
        [Output(Guid = "ca72b890-96df-40ee-bb64-240b54edf483")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

