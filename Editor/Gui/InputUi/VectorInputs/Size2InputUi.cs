using System.Linq;
using SharpDX;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Interaction;

namespace T3.Editor.Gui.InputUi.VectorInputs
{
    public class Size2InputUi : IntVectorInputValueUi<Size2>
    {
        public override bool IsAnimatable => true;

        public Size2InputUi() : base(2)
        {
        }

        public override IInputUi Clone()
        {
            return CloneWithType<Size2InputUi>();
        }

        protected override InputEditStateFlags DrawEditControl(string name, ref Size2 int3Value)
        {
            IntComponents[0] = int3Value.Width;
            IntComponents[1] = int3Value.Height;

            var inputEditState = VectorValueEdit.Draw(IntComponents, Min, Max, Scale, Clamp);
            int3Value = new Size2(IntComponents[0], IntComponents[1]);
            return inputEditState;
        }


        public override void ApplyValueToAnimation(IInputSlot inputSlot, InputValue inputValue, Animator animator, double time)
        {
            if (inputValue is not InputValue<Size2> typedInputValue)
                return;

            var curves = animator.GetCurvesForInput(inputSlot).ToArray();
            IntComponents[0] = typedInputValue.Value.Width;
            IntComponents[1] = typedInputValue.Value.Height;
            Curve.UpdateCurveValues(curves, time, IntComponents);
        }
    }
}