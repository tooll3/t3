namespace T3.Core.Operator.Types
{
    public class Add : Instance<Add>
    {
        [Output]
        public readonly Slot<float> Result = new Slot<float>();

        public Add()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Result.Value = Input1.GetValue(context) + Input2.GetValue(context);
        }

        [FloatInput(DefaultValue = 20.0f)]
        public readonly InputSlot<float> Input1 = new InputSlot<float>();

        [FloatInput(DefaultValue = 23.0f)]
        public readonly InputSlot<float> Input2 = new InputSlot<float>();
    }
}