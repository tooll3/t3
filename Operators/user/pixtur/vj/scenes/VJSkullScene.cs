using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.vj.scenes
{
    [Guid("78024fb9-4c07-400d-a919-8e1488dc37c5")]
    public class VJSkullScene : Instance<VJSkullScene>
    {

        [Output(Guid = "465eaee7-391e-4996-b25e-e73cff6245af")]
        public readonly Slot<Command> Output2 = new Slot<Command>();


    }
}

