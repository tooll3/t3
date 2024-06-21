using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.math.@int
{
	[Guid("6a4edb6a-5ced-4356-9090-4bf770cdeb52")]
    public class MultiplyInt : Instance<MultiplyInt>
    {
        [Output(Guid = "5E847363-142D-4DA9-A5B3-3A7AA2541BED")]
        public readonly Slot<int> Result = new();

        public MultiplyInt()
        {
            Result.UpdateAction += Update;
        }

        private void Update(EvaluationContext context)
        {
            Result.Value = A.GetValue(context) * B.GetValue(context);
        }


        [Input(Guid = "E010C56F-FF0B-44B6-BBD9-B50E2CCEC2BF")]
        public readonly InputSlot<int> A = new();

        [Input(Guid = "E02F9E84-A7BF-45BF-9CB1-0B0C1C396796")]
        public readonly InputSlot<int> B = new();
    }
}
