using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3f10f526_d8ca_4f67_95a4_d703b713088e
{
    public class LinkToMidiTime : Instance<LinkToMidiTime>
    {
        [Output(Guid = "c9e7a901-caa7-4eba-bacf-9eaea2fa85cb")]
        public readonly Slot<Command> Commands = new();


        [Input(Guid = "57e9c133-e213-43d8-b0ed-e73e9b046a18")]
        public readonly InputSlot<Command> SubGraph = new();

        [Input(Guid = "5969cc2b-654e-490e-ab66-1511483f50ae")]
        public readonly InputSlot<bool> ResyncTrigger = new();

    }
}

