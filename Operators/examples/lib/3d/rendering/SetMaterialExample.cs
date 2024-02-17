using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples._3d.rendering
{
	[Guid("5b86e841-548d-4dbd-a39b-6361e28e23f5")]
    public class SetMaterialExample : Instance<SetMaterialExample>
    {
        [Output(Guid = "a945d055-790b-4cca-856f-300850a6634e")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

