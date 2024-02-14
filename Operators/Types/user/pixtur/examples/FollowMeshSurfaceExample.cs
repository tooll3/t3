using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_a3cae073_d636_4537_9290_b1bdac4219d0
{
    public class FollowMeshSurfaceExample : Instance<FollowMeshSurfaceExample>
    {
        [Output(Guid = "597c8722-b113-4fe1-8500-be9c421bf893")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

