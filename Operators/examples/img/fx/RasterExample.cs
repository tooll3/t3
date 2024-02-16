using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.img.fx
{
	[Guid("014b8d6f-c7f2-43b5-84a8-033356e440ef")]
    public class RasterExample : Instance<RasterExample>
    {
        [Output(Guid = "8eedd2e2-8806-4e97-9ca2-ec6d881e62fc")]
        public readonly Slot<Texture2D> Output = new();

    }
}

