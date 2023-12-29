using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ff101c21_e166_466e_8582_84858789f3b6
{
    public class MarsExploration2 : Instance<MarsExploration2>
    {
        [Output(Guid = "021d44d2-96c0-4ea9-b41d-d37a703ba3fa")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

