using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f7fd7342_18d1_443a_98ec_758974891434
{
    public class MaxInt : Instance<MaxInt>
    {
        [Output(Guid = "0b6a3094-e7b3-4b61-a1d9-f220de67720a")]
        public readonly Slot<int> Result = new();


        public MaxInt()
        {
            Result.UpdateAction = Update;
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
