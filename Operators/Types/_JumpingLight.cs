using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_7d5e24dc_7f55_4156_8294_58e8a8bcce2b
{
    public class _JumpingLight : Instance<_JumpingLight>
    {
        [Output(Guid = "e7cd61d5-4d27-41fe-8778-bef25810ddca")]
        public readonly Slot<Command> Output = new Slot<Command>();


        [Input(Guid = "e3d3bc7e-703a-468d-bca7-f1492540441a")]
        public readonly InputSlot<Command> Command = new InputSlot<Command>();

    }
}

