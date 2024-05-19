using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_5317ade3_d4df_480d_872d_a17c63909da0
{
    public class CameraExample : Instance<CameraExample>
    {
        [Output(Guid = "0c2f45b5-7591-45a6-9861-ce2575444dd4")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

