namespace T3.Core.Operator.Types
{
    public class StringLength : Instance<StringLength>
    {
        [Output(Guid = "{C2FA7C57-6A0C-4D33-A70D-5130F3D52798}")]
        public readonly Slot<int> Length = new Slot<int>();

        public StringLength()
        {
            Length.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Length.Value = InputString.GetValue(context).Length;
        }

        [StringInput(DefaultValue = "Aber Hallo", Guid = "{5794D63A-3EF7-42C5-B726-E814EA9093E3}")]
        public readonly InputSlot<string> InputString = new InputSlot<string>();
    }
}