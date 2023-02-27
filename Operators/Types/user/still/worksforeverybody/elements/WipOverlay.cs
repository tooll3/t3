using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_b60f2321_0df5_42a7_bdcf_4660bbe549d6
{
    public class WipOverlay : Instance<WipOverlay>
    {
        [Output(Guid = "434a3d5c-96c3-4f5e-962e-b88a4415fce3")]
        public readonly Slot<Command> Output = new Slot<Command>();


    }
}

