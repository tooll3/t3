using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_5880cbc3_a541_4484_a06a_0e6f77cdbe8e
{
    public class AString : Instance<AString>
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
    }
}
