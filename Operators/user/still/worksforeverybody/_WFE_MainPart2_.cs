using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.still.worksforeverybody
{
	[Guid("8373fee8-b212-4118-8803-dd53b7c02bca")]
    public class _WFE_MainPart2_ : Instance<_WFE_MainPart2_>
    {
        [Output(Guid = "89c138d7-3662-4d1b-944a-66046242e8b6")]
        public readonly Slot<Command> Output = new();


    }
}

