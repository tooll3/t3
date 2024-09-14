using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Lib.point.draw
{
    [Guid("76cd7578-0f97-49a6-938a-caeaa98deaac")]
    public class ChunkInstancingExample : Instance<ChunkInstancingExample>
    {
        [Output(Guid = "95dee492-83bc-4b09-97bf-a8c7d20123ac")]
        public readonly Slot<T3.Core.DataTypes.Command> Update = new ();
    }
}

