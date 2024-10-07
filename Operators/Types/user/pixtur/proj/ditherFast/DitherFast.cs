using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_4238bc88_ca48_4eee_8e80_26423c6a65ac
{
    public class DitherFast : Instance<DitherFast>
    {
        [Output(Guid = "57c3b1da-226a-48e5-9be3-78bef388a9e2")]
        public readonly Slot<Texture2D> ImgOutput = new();


    }
}

