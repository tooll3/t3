using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib._3d.rendering
{
	[Guid("06a14a28-85b7-45a1-b885-2cc695c0e61d")]
    public class DepthOfFieldPipelineExample : Instance<DepthOfFieldPipelineExample>
    {
        [Output(Guid = "ed15f212-2354-459d-91ce-6cbfe278c809")]
        public readonly Slot<Command> Output = new Slot<Command>();


    }
}

