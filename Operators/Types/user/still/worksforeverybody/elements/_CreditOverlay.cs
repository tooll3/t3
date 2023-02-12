using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_cea732a4_c4c2_40df_a0ff_f209125e7c99
{
    public class _CreditOverlay : Instance<_CreditOverlay>
    {
        [Output(Guid = "4404e0a7-2470-4c14-8ebc-b61a641c9a6c")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "30825b6b-8fbd-4cbf-a56c-ec62a4e8948d")]
        public readonly InputSlot<string> Titles = new InputSlot<string>();

        [Input(Guid = "beb1768f-594a-4bef-b516-093161c5f1cc")]
        public readonly InputSlot<int> Index = new InputSlot<int>();

        [Input(Guid = "40420d46-f866-411e-a5fa-6d3996c6cf10")]
        public readonly InputSlot<float> Visibility = new InputSlot<float>();

        [Input(Guid = "69707763-2952-493d-8ccd-c41bf9b86ca0")]
        public readonly InputSlot<int> Style = new InputSlot<int>();


    }
}

