using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_48e284ff_cfcc_4d6c_83d4_256c9380de61
{
    public class OscOutputExample : Instance<OscOutputExample>
    {

        [Output(Guid = "bbf85f4a-f11f-47cf-8689-c059955117ce")]
        public readonly Slot<T3.Core.DataTypes.Command> OSCExecute = new Slot<T3.Core.DataTypes.Command>();

    }
}

