using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_08ad4c93_81ee_4c21_9bff_b5b03f7dd1f7
{
    public class BeatCounterTest : Instance<BeatCounterTest>
    {
        [Output(Guid = "cec882fa-5c80-4b86-b84b-430ebc46894d")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();


    }
}

