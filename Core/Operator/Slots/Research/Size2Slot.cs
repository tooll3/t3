using SharpDX;

namespace T3.Core.Operator.Slots.Research
{
    public class Size2Slot : InputSlot<Size2>
    {
        public Size2Slot(Size2 defaultValue) : base(defaultValue)
        {
            UpdateAction = Update;
            _bypassedUpdateAction = UpdateAction;
        }

        public Size2Slot(InputValue<Size2> typedInputValue) : base(typedInputValue)
        {
            UpdateAction = Update;
            _bypassedUpdateAction = UpdateAction;
        }

        public new void Update(EvaluationContext context)
        {
            if (InputConnection.Count > 0)
                Value = InputConnection[0].GetValue(context);
            else
            {
                if (Width.InputConnection != null)
                    Value.Width = Width.GetValue(context);
                if (Height.InputConnection != null)
                    Value.Height = Height.GetValue(context);
            }
        }

        public InputSlot<int> Width = new InputSlot<int>(new InputValue<int>(0));
        public InputSlot<int> Height = new InputSlot<int>(new InputValue<int>(0));
    }
}