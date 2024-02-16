using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.still.drd5
{
	[Guid("3ddae933-ed91-4773-af39-a35c89dcec11")]
    public class Nevoke : Instance<Nevoke>
    {
        [Output(Guid = "c6531a0b-0869-40f8-b677-bf8b550f4adb")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

