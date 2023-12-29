using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_604bd66e_f9ce_45f6_9fac_a8620418c73b
{
    public class VJTest : Instance<VJTest>
    {
        [Output(Guid = "07cf3c25-0cbb-414b-94b5-807c50c709d3")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

