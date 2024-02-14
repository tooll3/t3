using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_a3945dbe_d5be_4f0f_a904_8ee287d14a9f
{
    public class LoopExample : Instance<LoopExample>
    {
        [Output(Guid = "3cdfc9d7-bf03-4481-9869-ae8f43187304")]
        public readonly Slot<Command> Output = new();


    }
}

