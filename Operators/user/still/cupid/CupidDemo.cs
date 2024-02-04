using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.still.cupid
{
	[Guid("442d40e3-7c00-4161-a606-79c2fe6c36c1")]
    public class CupidDemo : Instance<CupidDemo>
    {
        [Output(Guid = "021568ee-42fc-4367-b652-0adb5397642e")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

