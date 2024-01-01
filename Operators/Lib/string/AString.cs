using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Operators.Lib.@string
{
	[Guid("5880cbc3-a541-4484-a06a-0e6f77cdbe8e")]
    public class AString : Instance<AString>, IExtractable
    {
        [Output(Guid = "dd9d8718-addc-49b1-bd33-aac22b366f94")]
        public readonly Slot<string> Result = new();

        public AString()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Result.Value = InputString.GetValue(context);
        }
        
        [Input(Guid = "ceeae47b-d792-471d-a825-49e22749b7b9")]
        public readonly InputSlot<string> InputString = new();

        public bool TryExtractInputsFor(IInputSlot inputSlot, out IEnumerable<ExtractedInput> inputParameters)
        {
            if (inputSlot is not InputSlot<string> stringSlot)
            {
                inputParameters = Array.Empty<ExtractedInput>();
                return false;
            }

            inputParameters = new[] { new ExtractedInput(InputString.Input, stringSlot.TypedInputValue) };
            return true;
        }
    }
}
