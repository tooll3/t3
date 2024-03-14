using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.point
{
	[Guid("3828afee-3ba2-43f4-abc0-6e8f3e257cc5")]
    public class TraceContourLinesExample : Instance<TraceContourLinesExample>
    {
        [Output(Guid = "81bd2032-53f8-4a8e-b67c-ed0b4aa6f9d8")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

