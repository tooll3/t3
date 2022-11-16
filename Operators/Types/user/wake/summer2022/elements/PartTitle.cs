using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_32feb968_6196_43ea_be23_958c21e884bc
{
    public class PartTitle : Instance<PartTitle>
    {

        [Output(Guid = "fc8b650a-0539-44e4-a110-3995fecc711d")]
        public readonly TimeClipSlot<Command> TimeOutput = new TimeClipSlot<Command>();

        [Input(Guid = "62a8185f-32c0-41d2-b8be-d8c1d7178c00")]
        public readonly InputSlot<string> Part = new InputSlot<string>();

        [Input(Guid = "C6407736-80ED-4E13-948C-8D20B50000A7")]
        public readonly InputSlot<string> Title = new InputSlot<string>();
    }
}

