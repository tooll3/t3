using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_76cd7578_0f97_49a6_938a_caeaa98deaac
{
    public class ChunkInstancingExample : Instance<ChunkInstancingExample>
    {
        [Output(Guid = "95dee492-83bc-4b09-97bf-a8c7d20123ac")]
        public readonly Slot<T3.Core.DataTypes.Command> Update = new ();
    }
}

