using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.img.generate
{
	[Guid("d71a564b-44c7-42a6-b0ca-05d5f512be14")]
    public class RyojiPattern1Example : Instance<RyojiPattern1Example>
    {
        [Output(Guid = "092308dc-05a0-4f10-815d-304a697908f7")]
        public readonly Slot<Texture2D> ImgOutput = new();


    }
}

