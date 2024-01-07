using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.still.worksforeverybody
{
	[Guid("d3e1ff33-8e76-4348-ac4a-cd537dfeb33f")]
    public class _WFE_MainPart2_backup1 : Instance<_WFE_MainPart2_backup1>
    {
        [Output(Guid = "de672f27-ea77-444b-91e0-f10c0a65662e")]
        public readonly Slot<Command> Output = new();


    }
}

