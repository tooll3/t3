using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user.wake.lpm2024teaser
{
	[Guid("a8e90e90-cb92-4009-811a-b97c06238b77")]
    public class Lpm2024Teaser : Instance<Lpm2024Teaser>
    {
        [Output(Guid = "778ec213-7df0-4696-a4d6-88b07a5c7dd1")]
        public readonly Slot<Texture2D> ImgOutput = new();


    }
}

