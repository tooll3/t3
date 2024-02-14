using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ee425946_3c6f_4bac_ab25_0e30571e8ca6
{
    public class NothingChanges : Instance<NothingChanges>
    {

        [Output(Guid = "24572cbd-ce42-4d53-9ff9-6780eb30e947")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output2 = new();

        [Input(Guid = "3ed34802-c8ca-4182-920e-aa476a256430")]
        public readonly InputSlot<Command> MoreContent = new();


    }
}

