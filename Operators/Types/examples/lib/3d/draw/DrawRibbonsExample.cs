using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_78ee50b0_910b_4edc_b8cd_5c24b2f1b7d9
{
    public class DrawRibbonsExample : Instance<DrawRibbonsExample>
    {
        [Output(Guid = "b443e732-759c-4fb9-959c-f12f7a17cbf7")]
        public readonly Slot<Texture2D> Output = new();


    }
}

