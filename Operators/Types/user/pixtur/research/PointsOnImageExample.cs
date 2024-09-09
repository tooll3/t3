using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f68fd746_0b4f_4f51_8d7d_f271922c36e8
{
    public class PointsOnImageExample : Instance<PointsOnImageExample>
    {
        [Output(Guid = "f22aa72c-19d2-4fd4-bae2-8eb3b2c0ba82")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

