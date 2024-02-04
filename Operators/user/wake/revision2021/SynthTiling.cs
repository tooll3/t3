using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.wake.revision2021
{
	[Guid("d6304632-a8c5-4029-8087-dc992b1f899c")]
    public class SynthTiling : Instance<SynthTiling>
    {

        [Output(Guid = "79c9eb66-e495-48e1-ae38-8410721ea1c5")]
        public readonly Slot<T3.Core.DataTypes.Command> Output = new();


    }
}

