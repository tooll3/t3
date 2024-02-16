using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.point
{
	[Guid("78413b72-9c04-41e7-93cc-7fc75aff99b5")]
    public class PointAttributeFromNoiseExample : Instance<PointAttributeFromNoiseExample>
    {
        [Output(Guid = "bd0734b4-a85d-49ae-9d33-70f78ae93214")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

