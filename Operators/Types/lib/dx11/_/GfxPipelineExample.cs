using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f0d36048_a112_4e0e_979e_d7dd9d99b197
{
    public class GfxPipelineExample : Instance<GfxPipelineExample>
    {
        [Output(Guid = "2bb6126e-15b6-457d-a00b-c14d00fc5d41")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();


    }
}

