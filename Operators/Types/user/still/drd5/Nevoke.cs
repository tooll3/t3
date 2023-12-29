using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3ddae933_ed91_4773_af39_a35c89dcec11
{
    public class Nevoke : Instance<Nevoke>
    {
        [Output(Guid = "c6531a0b-0869-40f8-b677-bf8b550f4adb")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

