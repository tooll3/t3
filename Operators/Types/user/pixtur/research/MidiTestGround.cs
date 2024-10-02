using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_db162ead_71a4_4835_95c8_6a719511314e
{
    public class MidiTestGround : Instance<MidiTestGround>
    {
        [Output(Guid = "c7d58d1d-5444-4e89-975e-0b2ebb9a90cf")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

