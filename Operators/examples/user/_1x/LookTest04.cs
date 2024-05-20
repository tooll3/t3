using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_d46236a7_b549_41b7_8b55_1b809310d191
{
    public class LookTest04 : Instance<LookTest04>
    {

        [Output(Guid = "4fd5be5e-ff6b-48f4-875a-066cd0703850")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();


    }
}

