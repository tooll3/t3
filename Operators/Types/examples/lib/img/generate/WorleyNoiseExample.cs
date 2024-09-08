using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_e3c3942b_451e_4e71_b6d8_ca5a6acd7ce1
{
    public class WorleyNoiseExample : Instance<WorleyNoiseExample>
    {
        [Output(Guid = "70d4e316-6d07-413d-a0c4-33714a63cd09")]
        public readonly Slot<Texture2D> ImgOutput = new Slot<Texture2D>();


    }
}

