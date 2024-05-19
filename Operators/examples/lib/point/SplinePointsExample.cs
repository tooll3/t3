using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_eb7bd521_c84b_45ad_88c6_a1ed79e64806
{
    public class SplinePointsExample : Instance<SplinePointsExample>
    {
        [Output(Guid = "e1e8ec79-d528-496e-a769-0d2c526b9cf0")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

