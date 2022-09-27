using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_91fb7506_c9c4_429c_a019_9ec47e3b1928
{
    public class NoiseClip : Instance<NoiseClip>
    {
        [Output(Guid = "8FCBA4E1-3ECC-461B-B4B8-6D1D341382AF")]
        public readonly TimeClipSlot<Texture2D> TextureOutput2 = new TimeClipSlot<Texture2D>();
        
        [Output(Guid = "591889d6-7a31-4dc2-a3e4-3daa08208280")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();


    }
}

