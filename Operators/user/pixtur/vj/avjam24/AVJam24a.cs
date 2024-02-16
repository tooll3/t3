using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.vj.avjam24
{
	[Guid("45615cbf-3cf5-46f3-b709-905b359b4362")]
    public class AVJam24a : Instance<AVJam24a>
    {
        [Output(Guid = "c9c264b5-e631-4e41-9823-29773e2a23f4")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

