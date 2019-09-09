using T3.Core.Operator;

namespace T3.Operators.Types
{
    public class FloatFormat : Instance<FloatFormat>
    {
        [Output(Guid = "{C63A1977-A594-490D-B5FB-DE4D40BAD016}")]
        public readonly Slot<string> Output = new Slot<string>();

        public FloatFormat()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Output.Value = Input.GetValue(context).ToString();
        }

        [Input(Guid = "{F36E4078-2608-4308-AB5F-077C05B1181A}")]
        public readonly InputSlot<float> Input = new InputSlot<float>();
    }
}