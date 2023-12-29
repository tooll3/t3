using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_4c496e8d_2a83_4493_a7a4_fdad29ef3f7d
{
    public class StringLength : Instance<StringLength>
    {
        [Output(Guid = "{C2FA7C57-6A0C-4D33-A70D-5130F3D52798}")]
        public readonly Slot<int> Length = new();

        public StringLength()
        {
            Length.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Length.Value = InputString.GetValue(context).Length;
        }

        [Input(Guid = "{5794D63A-3EF7-42C5-B726-E814EA9093E3}")]
        public readonly InputSlot<string> InputString = new();
    }
}