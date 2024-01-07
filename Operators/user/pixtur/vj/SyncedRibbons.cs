using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.vj
{
	[Guid("dda7898a-00c3-40c9-9fcd-020706969733")]
    public class SyncedRibbons : Instance<SyncedRibbons>
    {
        [Output(Guid = "8a54b659-97f7-4842-9a43-58188eb10759")]
        public readonly Slot<Command> Detection = new();


    }
}

