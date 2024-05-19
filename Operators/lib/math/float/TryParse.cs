using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_62cdbff3_cb29_4745_92b1_02490e35345d
{
    public class TryParse : Instance<TryParse>
    {
        [Output(Guid = "3e780cb0-eef5-4880-98b4-2004b573ddb6")]
        public readonly Slot<float> Result = new();

        public TryParse()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            
            if (float.TryParse(String.GetValue(context), result: out var result))
            {
                Result.Value = result;
            }
            else 
            {
                Result.Value = Default.GetValue(context);
            }
        }

        [Input(Guid = "36518523-aa14-4984-9796-5199b3ab1306")]
        public readonly InputSlot<string> String = new InputSlot<string>();

        [Input(Guid = "b66b998a-e2a7-4dce-9bcf-f409be3e08c9")]
        public readonly InputSlot<float> Default = new InputSlot<float>();
    }
}
