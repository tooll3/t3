using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.wake.sessions
{
	[Guid("30d1c825-c134-4d1c-a424-bb44abef6b6a")]
    public class WakeSession2023May15 : Instance<WakeSession2023May15>
    {
        [Output(Guid = "2fc60d26-4aa1-4351-817a-155153555f9e")]
        public readonly Slot<Texture2D> Texture = new();


    }
}

