using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_a9e36415_58b3_4e2c_b42a_757000d5e337
{
    public class VJUralt : Instance<VJUralt>
    {
        [Output(Guid = "c4484c6f-8a38-4cea-b71f-bbff12718054")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

