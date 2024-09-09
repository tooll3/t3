using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_fb5e9e3c_6ded_4e62_b456_28c8d5b29a1d
{
    public class RevisionEnds : Instance<RevisionEnds>
    {

        [Output(Guid = "3ea37eb9-446c-4078-bb0c-a602036233e3")]
        public readonly TimeClipSlot<Command> Output2 = new();


    }
}

