using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.wake.summer2022.elements
{
	[Guid("73496aee-5c15-4cfb-9303-c5c66df7caff")]
    public class WaitingScene : Instance<WaitingScene>
    {
        [Output(Guid = "ea9f83ca-e351-4c5f-8c5c-ed9509435719")]
        public readonly Slot<Command> Output = new();


    }
}

