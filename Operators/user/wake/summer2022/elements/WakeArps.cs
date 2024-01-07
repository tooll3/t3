using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.wake.summer2022.elements
{
	[Guid("814acb53-9a96-476f-b580-6eef174a318b")]
    public class WakeArps : Instance<WakeArps>
    {
        [Output(Guid = "e2301a1a-df21-42b2-88ec-5c5f4c705809")]
        public readonly Slot<Command> Output = new();


    }
}

