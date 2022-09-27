using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_fe22a34c_390e_4e7c_8dd8_2e341e7f2327
{
    public class AnimatedKaleidoscope : Instance<AnimatedKaleidoscope>
    {
        [Output(Guid = "b68d2405-821b-4347-bd59-2f62fa6bcf6f")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();


    }
}

