using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3a6fd508_0272_4c18_96b8_bc2387d3b2fd
{
    public class All : Instance<All>
    {
        [Output(Guid = "734bc5bc-caca-4367-abf5-a7ac94ed13d6")]
        public readonly Slot<bool> Result = new();

        public All()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var result = true;
            var anyConnected = false;
            
            foreach (var input in Input.GetCollectedTypedInputs())
            {
                anyConnected = true;
                result &= input.GetValue(context);
            }
            
            Result.Value = result & anyConnected;
        }

        [Input(Guid = "cf59ae3e-d111-479f-a42b-c5c014e65b32")]
        public readonly MultiInputSlot<bool> Input = new();
    }
}