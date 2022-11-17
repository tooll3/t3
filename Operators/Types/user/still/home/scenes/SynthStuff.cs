using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_aad38e92_c790_4ae3_981a_3ec28a0d8b80
{
    public class SynthStuff : Instance<SynthStuff>
    {

        [Output(Guid = "a0839f25-547b-4c7c-b3e2-2a424f7dbc96")]
        public readonly TimeClipSlot<Command> CommandClip = new TimeClipSlot<Command>();

    }
}

