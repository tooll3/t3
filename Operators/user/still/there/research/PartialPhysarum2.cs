using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.still.there.research
{
	[Guid("0998c5a8-8771-4161-801d-e14507c2e89c")]
    public class PartialPhysarum2 : Instance<PartialPhysarum2>
    {

        [Output(Guid = "7fa2fb31-dd11-48b1-b580-db5aa14aa7fb")]
        public readonly TimeClipSlot<Command> Output2 = new();


    }
}

