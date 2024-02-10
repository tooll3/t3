using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_d7fbe2ed_1aed_4cb3_adb8_ecd0c7b8cda0
{
    public class TomShadowParticleScene : Instance<TomShadowParticleScene>
    {

        [Output(Guid = "b3b26898-ee52-4268-86aa-d3d90c7fefd6")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

