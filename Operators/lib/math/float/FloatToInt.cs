using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_06b4728e_852c_491a_a89d_647f7e0b5415
{
    public class FloatToInt : Instance<FloatToInt>
    {
        [Output(Guid = "1EB7C5C4-0982-43F4-B14D-524571E3CDDA")]
        public readonly Slot<int> Integer = new();

        public FloatToInt()
        {
            Integer.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Integer.Value = (int)FloatValue.GetValue(context);
        }

        [Input(Guid = "AF866A6C-1AB0-43C0-9E8A-5D25C300E128")]
        public readonly InputSlot<float> FloatValue = new();
    }
}