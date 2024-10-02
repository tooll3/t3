using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_5b86e841_548d_4dbd_a39b_6361e28e23f5
{
    public class SetMaterialExample : Instance<SetMaterialExample>
    {
        [Output(Guid = "a945d055-790b-4cca-856f-300850a6634e")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

