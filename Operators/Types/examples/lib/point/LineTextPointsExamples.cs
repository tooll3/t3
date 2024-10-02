using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_c49ebc17_b730_4e86_9ea7_2c404e4be3ad
{
    public class LineTextPointsExamples : Instance<LineTextPointsExamples>
    {
        [Output(Guid = "0e338840-5515-42cc-b944-91dd73177b8d")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

