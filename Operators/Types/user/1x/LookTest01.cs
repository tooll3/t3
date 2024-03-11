using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_5d3ef365_dbeb_4d0a_b4b3_954e945d119f
{
    public class LookTest01 : Instance<LookTest01>
    {

        [Output(Guid = "750b2e4f-3ee7-433c-83e4-2c92010d73d8")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> output2D = new Slot<SharpDX.Direct3D11.Texture2D>();


    }
}

