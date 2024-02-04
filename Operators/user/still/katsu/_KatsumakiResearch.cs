using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.still.katsu
{
	[Guid("edf2f2fb-eaa3-4845-865d-679c5b1a0930")]
    public class _KatsumakiResearch : Instance<_KatsumakiResearch>
    {
        [Output(Guid = "00fa3e80-aabb-41db-b744-b2275918a3cc")]
        public readonly Slot<Texture2D> ImgOutput = new();


    }
}

