using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3828afee_3ba2_43f4_abc0_6e8f3e257cc5
{
    public class TraceContourLinesExample : Instance<TraceContourLinesExample>
    {
        [Output(Guid = "81bd2032-53f8-4a8e-b67c-ed0b4aa6f9d8")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

