using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.wake.summer2022.elements
{
	[Guid("32feb968-6196-43ea-be23-958c21e884bc")]
    public class PartTitle : Instance<PartTitle>
    {

        [Output(Guid = "fc8b650a-0539-44e4-a110-3995fecc711d")]
        public readonly TimeClipSlot<Command> TimeOutput = new();

        [Input(Guid = "62a8185f-32c0-41d2-b8be-d8c1d7178c00")]
        public readonly InputSlot<string> Part = new();

        [Input(Guid = "C6407736-80ED-4E13-948C-8D20B50000A7")]
        public readonly InputSlot<string> Title = new();
    }
}

