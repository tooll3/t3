using SharpDX.Direct3D11;
using T3.Core.Operator;

namespace T3.Operators.Types
{
    public class ComputeStageExample : Instance<ComputeStageExample>
    {
        [Output(Guid = "{405BB68E-9808-4AEA-9E05-C3486B3E045D}")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();
    }
}