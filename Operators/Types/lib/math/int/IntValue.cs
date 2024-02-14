using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_cc07b314_4582_4c2c_84b8_bb32f59fc09b
{
    public class IntValue : Instance<IntValue>
    {
        [Output(Guid = "8A65B34B-40BE-4DBF-812C-D4C663464C7F")]
        public readonly Slot<int> Result = new();

        public IntValue()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Result.Value = Int.GetValue(context);
        }
        
        [Input(Guid = "4515C98E-05BC-4186-8773-4D2B31A8C323")]
        public readonly InputSlot<int> Int = new();
    }
}
