namespace T3.Core.Operator.Types
{
    public class FloatFormat : Instance<FloatFormat>
    {
        [Output]
        public readonly Slot<string> Output = new Slot<string>();

        public FloatFormat()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Output.Value = Input.GetValue(context).ToString();
        }

        [FloatInput(DefaultValue = 3.0f)]
        public readonly InputSlot<float> Input = new InputSlot<float>();
    }
}