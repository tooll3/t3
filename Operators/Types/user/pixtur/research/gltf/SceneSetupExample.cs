using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_fa6d1930_b1f4_4655_8450_c876e8dd2803
{
    public class SceneSetupExample : Instance<SceneSetupExample>
    {
        [Output(Guid = "ce6a6d8c-a921-4c87-848b-83aecdff7684")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

