using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_5a4b23ff_588e_4dcc_833c_4fb5fb6fcb8f
{
    public class ForEachTest : Instance<ForEachTest>
    {
        [Output(Guid = "5dd58764-35ce-412d-a3ab-80f82dbeeccf")]
        public readonly IterationOutputSlot<System.Collections.Generic.List<string>> OutputList = new IterationOutputSlot<System.Collections.Generic.List<string>>();

        [Input(Guid = "3035a61a-c9fd-453a-84a9-442297860b39")]
        public readonly InputSlot<System.Collections.Generic.List<string>> Input = new InputSlot<System.Collections.Generic.List<string>>();
    }
}

