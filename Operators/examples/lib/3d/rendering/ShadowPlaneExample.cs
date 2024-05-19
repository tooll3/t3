using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_e20f492c_490f_4297_a9c8_0e5aab14f9c1
{
    public class ShadowPlaneExample : Instance<ShadowPlaneExample>
    {
        [Output(Guid = "50b1925c-2eca-469d-b5c1-065e01406160")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

