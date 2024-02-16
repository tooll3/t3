using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.still.worksforeverybody.elements
{
	[Guid("37b19195-5781-4f8d-af7b-c9ef6a4d146d")]
    public class _T3LogoAnimation : Instance<_T3LogoAnimation>
    {
        [Output(Guid = "4a5f3a14-29aa-47e6-8c31-3782df466a57")]
        public readonly Slot<Texture2D> Output = new();


    }
}

