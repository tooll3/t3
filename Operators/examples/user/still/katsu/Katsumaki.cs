using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user.still.katsu
{
	[Guid("bfa5b00c-e1f3-4b15-8fec-859156facfce")]
    public class Katsumaki : Instance<Katsumaki>
    {
        [Output(Guid = "b731afda-aff0-44e3-b0a9-b7caa2b73c86")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

