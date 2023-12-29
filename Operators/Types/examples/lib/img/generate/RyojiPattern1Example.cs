using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_d71a564b_44c7_42a6_b0ca_05d5f512be14
{
    public class RyojiPattern1Example : Instance<RyojiPattern1Example>
    {
        [Output(Guid = "092308dc-05a0-4f10-815d-304a697908f7")]
        public readonly Slot<Texture2D> ImgOutput = new();


    }
}

