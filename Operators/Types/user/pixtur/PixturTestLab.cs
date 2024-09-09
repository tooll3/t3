using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_a4f8b7e9_a52f_41b6_a63b_d30e5ba77825
{
    public class PixturTestLab : Instance<PixturTestLab>
    {
        [Output(Guid = "486eb33f-111d-4d6e-b81e-9f2ca16cb729")]
        public readonly Slot<Texture2D> Output = new();


    }
}

