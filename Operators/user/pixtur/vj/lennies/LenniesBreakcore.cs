using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.vj.lennies
{
	[Guid("758e28e9-e149-4a41-be5a-dcaad705345c")]
    public class LenniesBreakcore : Instance<LenniesBreakcore>
    {
        [Output(Guid = "975ae46a-f0bf-48d8-ad39-2cdd2ba9f8e9")]
        public readonly Slot<Texture2D> TextureOutput = new();


    }
}

