using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_d1530d89_32b9_4e16_97fe_c08d095d9d03
{
    public class MinInt : Instance<MinInt>
    {
        [Output(Guid = "82ff7dfe-e65f-415b-989c-8b478650b5d7")]
        public readonly Slot<int> Result = new();


        public MinInt()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var min = Int32.MaxValue;
            
            foreach (var i in Ints.CollectedInputs)
            {
                min = Math.Min(min, i.GetValue(context));
            }

            Result.Value = min;
        }


        [Input(Guid = "6bb2b4eb-f5d9-43f0-8584-4ea3d38f6538")]
        public readonly MultiInputSlot<int> Ints = new();
    }
}
