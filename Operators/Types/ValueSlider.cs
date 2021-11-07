using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_d6384148_c654_48ce_9cf4_9adccf91283a
{
    public class ValueSlider : Instance<ValueSlider>
    {
        [Output(Guid = "8078629f-0ef0-45da-a21c-e0140b5bd2d4")]
        public readonly Slot<float> Result = new Slot<float>();

        public ValueSlider()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var min = Min.GetValue(context);
            var max = Max.GetValue(context);
            var input = this.Input.GetValue(context);
            Result.Value = (max - min) * input + min;
        }

        [Input(Guid = "369cd898-ac86-4436-ac84-65672d923694")]
        public readonly InputSlot<float> Input = new InputSlot<float>();
        
        [Input(Guid = "c61b5165-0751-429e-8b3e-5346323f5270")]
        public readonly InputSlot<float> Min = new InputSlot<float>();

        [Input(Guid = "11063BFA-CC04-4978-9EFE-06859A3E6427")]
        public readonly InputSlot<float> Max = new InputSlot<float>();


    }
}
