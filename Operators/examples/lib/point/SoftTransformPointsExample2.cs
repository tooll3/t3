using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.point
{
	[Guid("a1384530-671b-4a6a-b17c-89480bc2f23a")]
    public class SoftTransformPointsExample2 : Instance<SoftTransformPointsExample2>
    {
        [Output(Guid = "95b440d5-9394-4687-b9a3-eaad83c0254c")]
        public readonly Slot<Texture2D> Output = new();


    }
}

