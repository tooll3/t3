using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_012119bf_aeec_4134_b7aa_6bc7f9816800
{
    public class SoundInputExample : Instance<SoundInputExample>
    {
        [Output(Guid = "e7ce8558-0fd1-4355-8c9e-dac6f0a3b757")]
        public readonly Slot<Texture2D> TextureOutput = new();


    }
}

