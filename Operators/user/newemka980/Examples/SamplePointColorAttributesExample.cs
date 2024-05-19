using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f4528476_298d_42b7_8138_3916bea2da6e
{
    public class SamplePointColorAttributesExample : Instance<SamplePointColorAttributesExample>
    {
        [Output(Guid = "7bd91d7a-6b16-4837-b43c-f3f99b66ac86")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

