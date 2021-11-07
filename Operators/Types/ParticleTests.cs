using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_4f33f57d_ddca_44ea_96d2_8897ff5da39b
{
    public class ParticleTests : Instance<ParticleTests>
    {
        [Output(Guid = "fe6ded6a-e31f-4b3d-b6e1-28c53695a9fd")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();

        [Output(Guid = "12c34caf-d266-405e-8038-95ff9c9834d7")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> DepthBuffer = new Slot<SharpDX.Direct3D11.Texture2D>();


    }
}

