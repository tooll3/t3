using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_245de58f_d3d0_4c57_ba27_1b884b7f6b31
{
    public class DrivenByMossExample : Instance<DrivenByMossExample>
    {

        [Output(Guid = "c16c7b14-2cb1-4160-b48f-d6a4e4619a53")]
        public readonly Slot<T3.Core.DataTypes.Command> OutputCommand = new Slot<T3.Core.DataTypes.Command>();

    }
}

