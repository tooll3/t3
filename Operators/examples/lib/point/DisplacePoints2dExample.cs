using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.point
{
	[Guid("9b5f602d-0449-453d-a08a-93430b313cb4")]
    public class DisplacePoints2dExample : Instance<DisplacePoints2dExample>
    {
        [Output(Guid = "3192d666-6b7c-4ef4-9086-684df2f387a0")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

