using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_cf1fdb46_72c0_4e3c_a00d_edef25ae955a
{
    public class FluidFireLogo : Instance<FluidFireLogo>
    {
        [Output(Guid = "80df5695-1d1e-46f3-ac5f-89a4294317d7")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

