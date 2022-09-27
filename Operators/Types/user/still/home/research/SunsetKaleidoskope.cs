using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_e0f15ac3_89f0_46ad_b69e_3d348c01b9c3
{
    public class SunsetKaleidoskope : Instance<SunsetKaleidoskope>
    {
        [Output(Guid = "e4e3c2fd-0f01-4e9e-9a0a-0ce718a81e3b")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();


    }
}

