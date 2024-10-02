using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_737a41db_bf35_4f66_8600_a083f0157cd5
{
    public class ColorGradeDepthExample : Instance<ColorGradeDepthExample>
    {
        [Output(Guid = "383f44bf-888c-413e-bb64-5400b30cfb70")]
        public readonly Slot<Texture2D> Output = new();


    }
}

