using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1984a755_39b0_4f0b_9c31_f2b67cab6db1
{
    public class RibbonsOnMeshExample : Instance<RibbonsOnMeshExample>
    {
        [Output(Guid = "b2e25cdf-a743-4e10-bf1f-c9b0f7474e11")]
        public readonly Slot<Texture2D> Output = new();


    }
}

