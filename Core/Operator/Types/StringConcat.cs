namespace T3.Core.Operator.Types
{
    public class StringConcat : Instance<StringConcat>
    {
        [Output(Guid = "{E47BF25E-351A-44E6-84C6-AD3ABC93531A}")]
        public readonly Slot<string> Result = new Slot<string>();

        public StringConcat()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Result.Value = Input1.GetValue(context) + Input2.GetValue(context);
        }

        [Input(Guid = "{56098B5B-FE9F-41B8-9BE3-F133A5309689}")]
        public readonly InputSlot<string> Input1 = new InputSlot<string>();

        [Input(Guid = "{6E9E070F-8700-4522-BE4E-477FB0D1A71F}")]
        public readonly InputSlot<string> Input2 = new InputSlot<string>();
    }
}