using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_e7821087_ec80_4c1c_907a_e3506dd345b3
{
    public class RevRibbons : Instance<RevRibbons>
    {

        [Output(Guid = "f916f73c-ef64-40b5-9994-04292e6a0a9a")]
        public readonly TimeClipSlot<Command> Output2 = new();


    }
}

