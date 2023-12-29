using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_351e27ae_cde8_4840_973c_2967023a254f
{
    public class MeteoriksEdit01 : Instance<MeteoriksEdit01>
    {
        [Output(Guid = "85ad2be4-3e3d-489a-9ad2-61503f3198b7")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

