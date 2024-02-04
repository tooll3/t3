using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.wake.mary
{
	[Guid("ba2a8670-e0c3-4e7b-9382-d1c0938ba2b3")]
    public class MaryTest2 : Instance<MaryTest2>
    {
        [Output(Guid = "690dbf18-90c8-499a-b1be-28093deefe4a")]
        public readonly Slot<Texture2D> Output = new();


    }
}

