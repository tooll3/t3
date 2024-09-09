using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9a34f503_709b_42e0_a25f_bc74573afa6b
{
    public class IsIntEven : Instance<IsIntEven>
    {
        [Output(Guid = "B69BC0BA-010D-4268-93F4-D5F682AF00D5")]
        public readonly Slot<bool> Result = new();

        public IsIntEven()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Result.Value = Value.GetValue(context) % 2 == 0;
        }
        
        [Input(Guid = "c5703990-1062-4512-b016-74ae1cce538a")]
        public readonly InputSlot<int> Value = new();
    }
}