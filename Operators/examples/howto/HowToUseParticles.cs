using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_812561d8_cbb5_40c3_a53e_3c3f0ad2352e
{
    public class HowToUseParticles : Instance<HowToUseParticles>
    {

        [Output(Guid = "a6f74a15-1f72-4e9c-955f-0711ff5f9c46")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output = new();


    }
}

