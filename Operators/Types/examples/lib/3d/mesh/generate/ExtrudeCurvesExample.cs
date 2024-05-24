using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_a469ca9f_a7f9_46b8_b34c_0a1c109e1222
{
    public class ExtrudeCurvesExample : Instance<ExtrudeCurvesExample>
    {
        [Output(Guid = "7661ebdf-0d10-437a-8b3e-71fac37cbd1a")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();

        [Input(Guid = "7c55d6c6-d712-4464-8dd8-2b7fac96e270")]
        public readonly InputSlot<bool> FixHoles = new InputSlot<bool>();


    }
}

