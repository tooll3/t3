using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_01ebb7a3_9caa_4259_aaa1_c79248b39325
{
    public class SamplePointAttributesExample2 : Instance<SamplePointAttributesExample2>
    {
        [Output(Guid = "acf384c2-66d8-4802-ab79-de2ac9eef9a4")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

