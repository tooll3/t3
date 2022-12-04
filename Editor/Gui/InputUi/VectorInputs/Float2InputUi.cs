using System.Linq;
using System.Numerics;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Interaction;

namespace T3.Editor.Gui.InputUi.VectorInputs
{
    public class Float2InputUi : FloatVectorInputValueUi<Vector2>
    {
        public Float2InputUi() : base(2) { }
        
        public override IInputUi Clone()
        {
            return CloneWithType<Float2InputUi>();
        }
        
        protected override InputEditStateFlags DrawEditControl(string name, ref Vector2 float2Value)
        {
            float2Value.CopyTo(FloatComponents);
            var inputEditState = VectorValueEdit.Draw(FloatComponents, Min, Max, Scale, Clamp);
            float2Value = new Vector2(FloatComponents[0], FloatComponents[1]);

            return inputEditState;
        }
        
        public override void ApplyValueToAnimation(IInputSlot inputSlot, InputValue inputValue, Animator animator, double time)
        {
            if (inputValue is not InputValue<Vector2> typedInputValue)
                return;
            
            var curves = animator.GetCurvesForInput(inputSlot).ToArray();
            typedInputValue.Value.CopyTo(FloatComponents);
            Curve.UpdateCurveValues(curves, time, FloatComponents);
        }
    }
}