using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.wake.summer2022
{
	[Guid("88af0668-6aae-419c-ace8-845c92135915")]
    public class WS2_Feedback : Instance<WS2_Feedback>
    {
        [Output(Guid = "7946bb83-39be-4a04-97ec-ee510f81c770")]
        public readonly Slot<Command> Output = new();


    }
}

