using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_17db8a36_079d_4c83_8a2a_7ea4c1aa49e6
{
    public class IntToFloat : Instance<IntToFloat>
    {
        [Output(Guid = "DB1073A1-B9D8-4D52-BC5C-7AE8C0EE1AC3")]
        public readonly Slot<float> Result = new();
        
        public IntToFloat()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Result.Value = IntValue.GetValue(context);
        }
        
        [Input(Guid = "01809B63-4B4A-47BE-9588-98D5998DDB0C")]
        public readonly InputSlot<int> IntValue = new();
    }
}