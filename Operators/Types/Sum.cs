using T3.Core.Operator;

namespace T3.Operators.Types
{
    public class Sum : Instance<Sum>
    {
        [Output(Guid = "{5CE9C625-F890-4620-9747-C98EAB4B9447}")]
        public readonly Slot<float> Result = new Slot<float>();

        public Sum()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Result.Value = 0;
            foreach (var input in Input.GetCollectedTypedInputs())
            {
                Result.Value += input.GetValue(context);
            }
        }

        [Input(Guid = "{AF4A49E6-1ECD-4E94-AE6D-FB5D2BC8430C}")]
        public readonly MultiInputSlot<float> Input = new MultiInputSlot<float>();
    }
}