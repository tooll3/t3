using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_61f2cf41_b520_4830_bc59_0d9b1d226274
{
    public class GlobeControlTest : Instance<GlobeControlTest>
    {
        [Output(Guid = "f39e3eda-7a9e-4336-ab81-df2cd2a1c844")]
        public readonly Slot<Texture2D> ImgOutput = new();


    }
}

