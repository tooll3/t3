using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_8e6ed99c_a3e0_42c0_9f81_a89b1e340757
{
    public class PaintExample : Instance<PaintExample>
    {
        [Output(Guid = "8cedd2ef-75a2-46d9-8a07-02491389a89f")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();

        [Input(Guid = "cc4b0b55-59c9-4898-b4ad-01568510e336")]
        public readonly InputSlot<bool> IsActve = new InputSlot<bool>();


    }
}

