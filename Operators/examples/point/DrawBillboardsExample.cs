using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.point
{
	[Guid("93292762-bc39-4b66-ace0-583f461abf76")]
    public class DrawBillboardsExample : Instance<DrawBillboardsExample>
    {
        [Output(Guid = "de29b6ab-a646-4403-9fd0-d3018be62559")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

