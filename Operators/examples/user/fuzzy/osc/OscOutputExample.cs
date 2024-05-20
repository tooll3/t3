using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user.fuzzy.osc
{
    [Guid("48e284ff-cfcc-4d6c-83d4-256c9380de61")]
    public class OscOutputExample : Instance<OscOutputExample>
    {

        [Output(Guid = "bbf85f4a-f11f-47cf-8689-c059955117ce")]
        public readonly Slot<T3.Core.DataTypes.Command> OSCExecute = new Slot<T3.Core.DataTypes.Command>();

    }
}

