using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_135fd07f_b9a1_47d9_acc5_c8a15ae7558a
{
    public class StreamCountDown : Instance<StreamCountDown>
    {
        [Output(Guid = "2151f4b3-34fe-4d55-9669-ec3aea47a44c")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

