using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.still.there.scenes
{
	[Guid("6681f137-c653-4b48-a7ca-504b0056d3fc")]
    public class WispsForrest1 : Instance<WispsForrest1>
    {

        [Output(Guid = "2763f9d3-3f56-4dd6-9b5a-c428f1a32a42")]
        public readonly TimeClipSlot<Command> Output2 = new();


    }
}

