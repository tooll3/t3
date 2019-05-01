namespace T3.Core.Operator.Types
{
    public class StringLength : Instance<StringLength>
    {
        [Output]
        public readonly Slot<int> Length = new Slot<int>();

        public StringLength()
        {
            Length.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Length.Value = InputString.GetValue(context).Length;
        }

        [StringInput(DefaultValue = "Aber Hallo")]
        public readonly InputSlot<string> InputString = new InputSlot<string>();
    }
}