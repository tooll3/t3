using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.math.@int
{
	[Guid("f7fd7342-18d1-443a-98ec-758974891434")]
    public class MaxInt : Instance<MaxInt>
    {
        [Output(Guid = "0b6a3094-e7b3-4b61-a1d9-f220de67720a")]
        public readonly Slot<int> Result = new();


        public MaxInt()
        {
            Result.UpdateAction += Update;
        }

        private void Update(EvaluationContext context)
        {
            var max = Int32.MinValue;
            
            foreach (var i in Ints.CollectedInputs)
            {
                max = Math.Max(max, i.GetValue(context));
            }

            Result.Value = max;
        }


        [Input(Guid = "286DACDF-A469-4983-A944-D9F34ED1E7DE")]
        public readonly MultiInputSlot<int> Ints = new();
    }
}
