using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_e14af8a3_8672_4348_af9e_735714c31c92
{
    public class SetVJVariables : Instance<SetVJVariables>
    {
        [Output(Guid = "741a3753-2021-411f-b3ea-000edd548aeb")]
        public readonly Slot<Command> Output = new Slot<Command>();


        [Input(Guid = "4aaf265f-2c98-4fc2-8cb7-ea1438dcfef4")]
        public readonly InputSlot<Command> SubGraph = new InputSlot<Command>();

    }
}

