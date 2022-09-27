using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_fe6fde44_e6b3_4e2a_b8fa_64c5c5416a04
{
    public class AudioReactionGlowExample : Instance<AudioReactionGlowExample>
    {
        [Output(Guid = "1e997341-5a00-4fc3-a6bb-289dbed546e7")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

