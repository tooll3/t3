using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user.still.meteoriks23
{
	[Guid("7d58e562-3465-4b7f-a153-fffed2d150d5")]
    public class MeteoriksEdit03 : Instance<MeteoriksEdit03>
    {
        [Output(Guid = "6735cb37-c575-41b6-89ad-bf22019b8a21")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

