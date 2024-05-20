using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user._1x
{
    [Guid("5d3ef365-dbeb-4d0a-b4b3-954e945d119f")]
    public class LookTest01 : Instance<LookTest01>
    {

        [Output(Guid = "750b2e4f-3ee7-433c-83e4-2c92010d73d8")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> output2D = new Slot<SharpDX.Direct3D11.Texture2D>();


    }
}

