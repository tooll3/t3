using System.Linq;
using System.Numerics;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Interaction;


namespace T3.Gui.InputUi
{
    public class Float3InputUi : FloatVectorInputValueUi<Vector3>
    {
        public Float3InputUi() : base(3) { }

        public override IInputUi Clone()
        {
            return CloneWithType<Float3InputUi>();
        }

        protected override InputEditStateFlags DrawEditControl(string name, ref Vector3 float3Value)
        {
            float3Value.CopyTo(FloatComponents);
            var inputEditState = VectorValueEdit.Draw(FloatComponents, Min, Max, Scale, Clamp);
            float3Value = new Vector3(FloatComponents[0], FloatComponents[1], FloatComponents[2]);

            return inputEditState;
        }
        
        public override void ApplyValueToAnimation(IInputSlot inputSlot, InputValue inputValue, Animator animator, double time)
        {
            if (inputValue is not InputValue<Vector3> typedInputValue)
                return;
            
            var curves = animator.GetCurvesForInput(inputSlot).ToArray();
            typedInputValue.Value.CopyTo(FloatComponents);
            Curve.UpdateCurveValues(curves, time, FloatComponents);
        }
    }
}