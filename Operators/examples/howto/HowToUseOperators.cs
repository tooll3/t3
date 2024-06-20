using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.howto
{
	[Guid("fc3a89d4-e7ea-45d8-b1c5-41170c7cd2b8")]
    public class HowToUseOperators: Instance<HowToUseOperators>
    {
        [Output(Guid = "2621700c-8798-40ea-9dc8-a3f5ed1d0f41")]
        public readonly Slot<Texture2D> Texture = new();


    }
}

