using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_fc2479e3_4ad5_4ba5_9077_778d1b04ece0
{
    public class DOFTestScene : Instance<DOFTestScene>
    {
        [Output(Guid = "5723058b-7b62-4ec0-8954-789822c856fd", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new Slot<Command>();


    }
}

