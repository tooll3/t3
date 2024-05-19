using System.Globalization;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_416a1c8d_4143_4de7_bcf9_466c4009112a
{
    public class IntToString : Instance<IntToString>
    {
        [Output(Guid = "460f2a09-b706-4e1e-a1fb-32eeab51524b")]
        public readonly Slot<string> Output = new();

        public IntToString()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var v = Value.GetValue(context);
            var s = Format.GetValue(context);
            try
            {
                Output.Value = string.IsNullOrEmpty(s) ? v.ToString(CultureInfo.InvariantCulture) : string.Format(CultureInfo.InvariantCulture, s, v);
            }
            catch (System.FormatException)
            {
                Output.Value = "Invalid Format";
            }
        }

        [Input(Guid = "3C1A8975-AB27-40B3-9FEE-7DE38643294A")]
        public readonly InputSlot<int> Value = new();

        [Input(Guid = "a9dd6368-455d-4b1d-8685-02d8f7640ecf")]
        public readonly InputSlot<string> Format = new();
    }
}