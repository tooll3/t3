using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_eae8b8af_ce79_4e0a_9777_2dd0a99c18cb
{
    public class IntDiv : Instance<IntDiv>
    {
        [Output(Guid = "3BB1068A-04EF-4804-83F8-3A3EE6922E2F")]
        public readonly Slot<int> Result = new();

        public IntDiv()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var n = Numerator.GetValue(context);
            var d = Denominator.GetValue(context);
            Result.Value = (d == 0) ? 1 : n / d;
        }
        
        [Input(Guid = "95AAAA60-5582-40B0-907D-74A39710C006")]
        public readonly InputSlot<int> Numerator = new();

        [Input(Guid = "996CC728-62AB-4C77-B454-59F0D2F25C00")]
        public readonly InputSlot<int> Denominator = new();
    }
}
