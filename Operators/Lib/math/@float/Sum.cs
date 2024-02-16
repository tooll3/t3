using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.math.@float
{
	[Guid("2f851b5b-b66d-40b0-9445-e733dc4b907d")]
    public class Sum : Instance<Sum>
    {
        [Output(Guid = "{5CE9C625-F890-4620-9747-C98EAB4B9447}")]
        public readonly Slot<float> Result = new();

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
            Input.DirtyFlag.Clear();
        }

        [Input(Guid = "{AF4A49E6-1ECD-4E94-AE6D-FB5D2BC8430C}")]
        public readonly MultiInputSlot<float> Input = new();
    }
}