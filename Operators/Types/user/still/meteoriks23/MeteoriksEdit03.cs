using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_7d58e562_3465_4b7f_a153_fffed2d150d5
{
    public class MeteoriksEdit03 : Instance<MeteoriksEdit03>
    {
        [Output(Guid = "6735cb37-c575-41b6-89ad-bf22019b8a21")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

