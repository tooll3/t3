using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_25bf7552_68fa_463a_a0f6_8138b8688f9a
{
    public class ComputeStageExample : Instance<ComputeStageExample>
    {
        [Output(Guid = "{405BB68E-9808-4AEA-9E05-C3486B3E045D}")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();
    }
}