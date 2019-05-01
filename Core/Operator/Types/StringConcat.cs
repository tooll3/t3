namespace T3.Core.Operator.Types
{
    public class StringConcat : Instance<StringConcat>
    {
        [Output]
        public readonly Slot<string> Result = new Slot<string>();

        public StringConcat()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Result.Value = Input1.GetValue(context) + Input2.GetValue(context);
        }

        [StringInput(DefaultValue = "")]
        public readonly InputSlot<string> Input1 = new InputSlot<string>();

        [StringInput(DefaultValue = "")]
        public readonly InputSlot<string> Input2 = new InputSlot<string>();
    }
}