using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.io.midi
{
	[Guid("fd5467c7-c75d-4755-8885-fd1ff1f07c95")]
    public class SelectFloatFromDict : Instance<SelectFloatFromDict>
    {
        [Output(Guid = "4b281a08-46e9-4036-9a80-29caf11e3b6c")]
        public readonly Slot<float> Result = new(0f);

        [Input(Guid = "126D52EB-CDF9-48E6-AC77-BB6E90700C56")]
        public readonly InputSlot<Dict<float>> DictionaryInput = new();

        [Input(Guid = "B0ACB8AD-9F90-4908-B780-1297E0A1D572")]
        public readonly InputSlot<string> Select = new();

        public SelectFloatFromDict() : base()
        {
            Result.Value = 0f;
            Result.UpdateAction += Update;
        }

        private void Update(EvaluationContext context)
        {
            _dict = DictionaryInput.GetValue(context);
            _selectCommand = Select.GetValue(context);
            if (_dict != null)
                _dict.TryGetValue(_selectCommand, out Result.Value);
        }

        private Dict<float> _dict;
        private string _selectCommand;
    }
}
