using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_56426136_3c66_4246_89e6_737f8fc8f8c9
{
    public class FloatSelector : Instance<FloatSelector>
    {

        [Output(Guid = "f7493e26-69dc-488f-9eae-f3a291750684")]
        public readonly Slot<float> SelectedValue = new Slot<float>();

        [Input(Guid = "c9c651d2-b456-43fc-bbdb-583018e4f2bd")]
        public readonly MultiInputSlot<float> Values = new MultiInputSlot<float>();

        [Input(Guid = "94c5cdf6-33b6-43cc-a861-a8a1a4885096")]
        public readonly MultiInputSlot<int> SelectedIndex = new MultiInputSlot<int>();

        [Input(Guid = "2a2f177c-f5c6-4538-a99a-9b2eb37f2269")]
        public readonly InputSlot<bool> Wrap = new InputSlot<bool>();

    }
}

