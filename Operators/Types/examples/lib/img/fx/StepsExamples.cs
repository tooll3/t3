using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_47ee078b_e24f_4493_a068_864938e2c90b
{
    public class StepsExamples : Instance<StepsExamples>
    {

        [Output(Guid = "1a00cbdb-8ead-4c34-92f2-2da3c73571c2")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> ImageOutput = new();


    }
}

