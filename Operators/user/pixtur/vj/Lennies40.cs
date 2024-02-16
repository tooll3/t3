using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.vj
{
	[Guid("80908e49-b0da-4785-acd6-16bac1f51d09")]
    public class Lennies40 : Instance<Lennies40>
    {
        [Output(Guid = "15748082-7ee7-4392-b107-3b9e9a38dd88")]
        public readonly Slot<Command> Result = new();


    }
}

