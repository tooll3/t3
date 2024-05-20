using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.vj.scenes
{
    [Guid("8309d82d-7a78-45c7-8b00-b9d82c4b36a2")]
    public class VJFaceBlobs : Instance<VJFaceBlobs>
    {
        [Output(Guid = "cb264e90-1fe5-4418-99bc-5bd0fc98b0eb")]
        public readonly Slot<Command> Output = new Slot<Command>();


    }
}

