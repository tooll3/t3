using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_690c1fe5_c189_40c7_a6eb_6bc282b4b0a1
{
    public class ReplicateAtPointExample : Instance<ReplicateAtPointExample>
    {
        [Output(Guid = "b09cbc30-53a7-4f7c-9fcc-889a36791c49")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

