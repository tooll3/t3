using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1ec749af_fe7d_4728_9855_d1fa3e879751
{
    public class HowToUseTooll : Instance<HowToUseTooll>
    {
        [Output(Guid = "c301380c-8fe6-4e3d-af10-9cebd230b0e9")]
        public readonly Slot<Texture2D> TextureOutput = new();


    }
}

