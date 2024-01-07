using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.dailies
{
	[Guid("6c22e96f-bd1f-486a-a10f-a174a057f721")]
    public class PulseLaserTest : Instance<PulseLaserTest>
    {
        [Output(Guid = "bc4d8742-0952-4620-8404-1ed46a75da9e")]
        public readonly Slot<Texture2D> ImgOutput = new();


    }
}

