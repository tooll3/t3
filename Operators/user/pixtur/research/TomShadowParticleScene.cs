using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.research
{
	[Guid("d7fbe2ed-1aed-4cb3-adb8-ecd0c7b8cda0")]
    public class TomShadowParticleScene : Instance<TomShadowParticleScene>
    {

        [Output(Guid = "b3b26898-ee52-4268-86aa-d3d90c7fefd6")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

