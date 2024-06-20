using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.examples
{
	[Guid("08e40de8-aa4d-48d9-978d-690cd687220c")]
    public class FrameClock : Instance<FrameClock>
    {
        [Output(Guid = "de88afb2-9ebc-4d7a-994e-af62b0c56cfc")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

