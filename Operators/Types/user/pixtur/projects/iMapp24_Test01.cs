using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Types.user.pixtur.projects
{
    [Guid("423a044b-0490-48bf-ba9f-764c29cb3767")]
    public class iMapp24_Test01 : Instance<iMapp24_Test01>
    {
        [Output(Guid = "b009806b-dd73-40d8-b881-b20b70cc83ad")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "5625620e-7cda-4677-8fdb-23f97389e992")]
        public readonly InputSlot<float> Size = new InputSlot<float>();


    }
}

