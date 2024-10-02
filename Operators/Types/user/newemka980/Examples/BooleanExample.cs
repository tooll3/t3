using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9913adf3_8fe5_4d85_95ab_f04439c6edcb
{
    public class BooleanExample : Instance<BooleanExample>
    {
        [Output(Guid = "5b42af7d-3128-498e-8b96-cc707101e9e3")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

