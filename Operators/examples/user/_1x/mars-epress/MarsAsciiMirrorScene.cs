using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user._1x.marsEpress
{
    [Guid("df176113-ccc4-43e7-a462-5f37fff72fd2")]
    public class MarsAsciiMirrorScene : Instance<MarsAsciiMirrorScene>
    {
        [Output(Guid = "491dc8f6-8688-4b21-92f7-a92857f40539")]
        public readonly Slot<Command> Output = new();

    }
}

