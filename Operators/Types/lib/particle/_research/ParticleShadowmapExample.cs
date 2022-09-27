using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_7a5e8169_601e_4fd8_8ce4_bff669f50d37
{
    public class ParticleShadowmapExample : Instance<ParticleShadowmapExample>
    {
        [Output(Guid = "229cf691-ee88-4e84-ad4f-90c51ecb14df")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();


    }
}

