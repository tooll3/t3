using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.wake.summer2022.elements
{
	[Guid("8f58aae2-e30b-4239-b5c0-8da09c1e84c1")]
    public class WakeSplashes : Instance<WakeSplashes>
    {
        [Output(Guid = "1a550192-13c8-403c-b5a3-1ee559b90e13")]
        public readonly Slot<Command> Output = new();


    }
}

