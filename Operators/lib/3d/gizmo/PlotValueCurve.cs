using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_92f3193e_a7dd_4417_b569_129823607fbe
{
    public class PlotValueCurve : Instance<PlotValueCurve>
    {
        [Output(Guid = "2af722b5-3af1-448a-9d0b-2a8463744a99")]
        public readonly Slot<Command> Output = new();


        [Input(Guid = "491bba32-90df-477c-b9d6-0b22bad8b94e")]
        public readonly InputSlot<float> Value = new();

        [Input(Guid = "04c1a4d9-66f0-4d64-aed0-9b960c2bcbb7")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new();

        [Input(Guid = "622420b3-d5d5-4514-962e-6eaa04f34484")]
        public readonly InputSlot<string> Label = new();

        [Input(Guid = "9fe97c16-b8ba-41a0-be15-a60d5124d806")]
        public readonly InputSlot<float> RangeMin = new();

        [Input(Guid = "05ea7714-a4c5-4845-9fed-957f5fa1b95e")]
        public readonly InputSlot<float> RangeMax = new();

        [Input(Guid = "12a0a526-6255-49ee-a532-0e774dab91f8")]
        public readonly InputSlot<bool> Reset = new();

        [Input(Guid = "932d5873-a16d-447f-a93a-6fb456c76256")]
        public readonly InputSlot<int> BufferLength = new InputSlot<int>();

    }
}

