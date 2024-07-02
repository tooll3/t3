using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_0f09816e_b26c_4236_815a_5f9953957b05
{
    public class GamepadInputExample : Instance<GamepadInputExample>
    {
        [Output(Guid = "fdad7c0f-98fd-4284-9b84-650a9d1fb13d")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

