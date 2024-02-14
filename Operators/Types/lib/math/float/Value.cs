using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_5d7d61ae_0a41_4ffa_a51d_93bab665e7fe
{
    public class Value : Instance<Value>
    {
        [Output(Guid = "f83f1835-477e-4bb6-93f0-14bf273b8e94")]
        public readonly Slot<float> Result = new();

        public Value()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Result.Value = Float.GetValue(context);
        }
        
        [Input(Guid = "7773837e-104a-4b3d-a41f-cadbd9249af2")]
        public readonly InputSlot<float> Float = new();
        
    }
}
