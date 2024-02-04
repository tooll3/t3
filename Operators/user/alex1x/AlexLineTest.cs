using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.alex1x
{
	[Guid("27eb35ad-837a-43d6-a4bd-e8dfe3661070")]
    public class AlexLineTest : Instance<AlexLineTest>
    {
        [Output(Guid = "6727f7ff-a420-4072-8ee5-4a6ce77e9603")]
        public readonly Slot<Texture2D> ImgOutput = new();


    }
}

