using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.still.worksforeverybody
{
	[Guid("edc5e1e6-b850-485a-99e1-7eaa070cc301")]
    public class WFE_RoughtCut : Instance<WFE_RoughtCut>
    {
        [Output(Guid = "fa4b7416-a6e0-4c75-ba3d-beaa6682d1e6")]
        public readonly Slot<Texture2D> TextureOutput = new();


    }
}

