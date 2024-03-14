using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.math.vec2
{
	[Guid("3dd015ed-5d7a-4b0e-a6da-958c58f716bb")]
    public class GridPositionExample : Instance<GridPositionExample>
    {
        [Output(Guid = "a2de29e8-2a27-47c9-b99e-5b1ce4dc10b5")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

