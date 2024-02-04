using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.still.there.scenes
{
	[Guid("6231f780-c66f-452d-ba1c-4b5e21efc97d")]
    public class IntoTheWhiteScene : Instance<IntoTheWhiteScene>
    {

        [Output(Guid = "1cd5f974-e4dc-46b7-a9e2-829938022644")]
        public readonly TimeClipSlot<Command> Output2 = new();


    }
}

