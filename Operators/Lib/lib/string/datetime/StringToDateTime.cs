using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_a78a07f8_cf75_4a72_8952_b9ba40d6983f
{
    public class StringToDateTime : Instance<StringToDateTime>, IStatusProvider
    {
        [Output(Guid = "8D2981DD-AC26-4CC0-8646-DEFB7196085C")]
        public readonly Slot<DateTime> Output = new();
        

        public StringToDateTime()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var dateString = DateString.GetValue(context);
            if (DateTime.TryParse(dateString, out var dateTime))
            {
                Output.Value = dateTime;
                _lastStatusError = null;
            }
            else
            {
                _lastStatusError = $"Failed to parse {dateString} into DateTime";
            }
        }

        private string _lastStatusError;

        // [Input(Guid = "d4f58293-ac29-4dee-8a66-a56724bf7006")]
        // public readonly InputSlot<DateTime> Value = new InputSlot<DateTime>();

        [Input(Guid = "5F92445D-9234-420E-919D-21CDB6FB587D")]
        public readonly InputSlot<string> DateString = new();
        
        // [Input(Guid = "62d27f4c-ce64-497b-bca6-5a36bfd4232c")]
        // public readonly InputSlot<string> Format = new InputSlot<string>();

        public IStatusProvider.StatusLevel GetStatusLevel()
        {
            return string.IsNullOrEmpty(_lastStatusError) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Warning;
        }

        public string GetStatusMessage()
        {
            return _lastStatusError;
        }
    }
}