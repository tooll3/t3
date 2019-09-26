using SharpDX.Direct3D11;
using T3.Core.Operator;

namespace T3.Operators.Types
{
    public class ComputeWobble : Instance<ComputeWobble>
    {
        [Output(Guid = "{F4E9BC98-6ED2-48A2-A2B6-A3372A74EA1B}")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();

        [Input(Guid = "{0214FD61-536B-4A58-9236-00466698269F}")]
        public readonly InputSlot<float> Speed = new InputSlot<float>();

        [Input(Guid = "{83771C7C-F861-4533-B4D2-3D9782A4C973}")]
        public readonly InputSlot<float> Strength = new InputSlot<float>();
    }
}