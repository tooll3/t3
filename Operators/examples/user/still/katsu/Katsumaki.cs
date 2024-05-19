using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_bfa5b00c_e1f3_4b15_8fec_859156facfce
{
    public class Katsumaki : Instance<Katsumaki>
    {
        [Output(Guid = "b731afda-aff0-44e3-b0a9-b7caa2b73c86")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

