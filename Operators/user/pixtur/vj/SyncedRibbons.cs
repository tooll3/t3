using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_dda7898a_00c3_40c9_9fcd_020706969733
{
    public class SyncedRibbons : Instance<SyncedRibbons>
    {
        [Output(Guid = "8a54b659-97f7-4842-9a43-58188eb10759")]
        public readonly Slot<Command> Detection = new();


    }
}

