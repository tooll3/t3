using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_500ee741_bd04_4e13_9e40_53ec72af1bea
{
    public class MeetChris2 : Instance<MeetChris2>
    {
        [Output(Guid = "9a307791-1c20-46c3-8ffc-0e834c2fccf1")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

