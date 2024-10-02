using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_c6f1bd33_7a38_43d0_bfaf_337cf59fcdb9
{
    public class LazersMain : Instance<LazersMain>
    {
        [Output(Guid = "537993d1-f651-454a-b60b-652206d6fc4e")]
        public readonly Slot<Texture2D> ImgOutput = new();


    }
}

