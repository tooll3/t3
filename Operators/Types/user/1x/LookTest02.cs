using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_701f78b9_5b1a_4332_ab5e_89bcb1f520ce
{
    public class LookTest02 : Instance<LookTest02>
    {

        [Output(Guid = "1f530af1-4348-4000-b748-1ec1cbfcfb53")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> output2D = new Slot<SharpDX.Direct3D11.Texture2D>();


    }
}

