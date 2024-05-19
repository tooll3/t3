using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_21198ce9_7ef8_4f5e_a26a_f29b6abbcdec
{
    public class DrawConnectionLinesExample : Instance<DrawConnectionLinesExample>
    {
        [Output(Guid = "3c42f59b-1595-423b-b605-2c6f24ff6ed5")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

