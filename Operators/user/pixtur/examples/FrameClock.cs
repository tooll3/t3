using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_08e40de8_aa4d_48d9_978d_690cd687220c
{
    public class FrameClock : Instance<FrameClock>
    {
        [Output(Guid = "de88afb2-9ebc-4d7a-994e-af62b0c56cfc")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

