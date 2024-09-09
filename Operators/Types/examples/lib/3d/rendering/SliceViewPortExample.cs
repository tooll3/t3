using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_81337795_7e15_4335_a067_6d2c54a7b4b8
{
    public class SliceViewPortExample : Instance<SliceViewPortExample>
    {
        [Output(Guid = "328c2eb7-80bf-4a64-aef0-fb0c8bb72ed0")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

