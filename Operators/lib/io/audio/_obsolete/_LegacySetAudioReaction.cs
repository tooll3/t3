using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_dd00cb1b_1c0e_4e79_9ea2_4b23686c6f37
{
    public class _LegacySetAudioReaction : Instance<_LegacySetAudioReaction>
    {
        [Output(Guid = "aab8fbed-1ffa-4d5b-b488-a458e45844d0")]
        public readonly Slot<Command> Output = new();


        [Input(Guid = "d243af8f-7b1d-4110-912b-2c430726becc")]
        public readonly MultiInputSlot<Command> Command = new();

        [Input(Guid = "a8900010-bf6d-44e4-bd72-28427a5dadd8")]
        public readonly InputSlot<int> InputSource = new();

    }
}

