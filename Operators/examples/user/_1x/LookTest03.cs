using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_4bc2ca61_54c3_4a6b_a8ae_619a79395870
{
    public class LookTest03 : Instance<LookTest03>
    {

        [Output(Guid = "afb72dc5-df44-4ae5-84cc-b14d94be5d88")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> output2D = new Slot<SharpDX.Direct3D11.Texture2D>();


    }
}

