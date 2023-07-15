using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_aaf19aea_06f6_4c5b_8765_9910f8ed7ad0
{
    public class SyncedRandomScroller : Instance<SyncedRandomScroller>
    {
        [Output(Guid = "27b856b4-11dc-40af-8823-ed43e69d447f")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "b6e11e35-ea50-493c-9be1-7d328f54807c")]
        public readonly InputSlot<bool> RandomTrigger = new InputSlot<bool>();

        [Input(Guid = "69795161-4890-495b-9298-3ef6c0a1cd81")]
        public readonly InputSlot<bool> RandomTriggerB = new InputSlot<bool>();

        [Input(Guid = "f59234de-a3a7-43b7-9fce-05716515e278")]
        public readonly InputSlot<int> MaxLength = new InputSlot<int>();


    }
}

