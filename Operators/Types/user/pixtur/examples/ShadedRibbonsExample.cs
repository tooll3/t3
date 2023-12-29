using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_4e95a5e8_a075_4493_9aaa_48ea181198e2
{
    public class ShadedRibbonsExample : Instance<ShadedRibbonsExample>
    {
        [Output(Guid = "59996d13-ffca-4817-9514-379ebde296fe")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

