using System.Linq;
using System.Numerics;
using Editor.Gui.Interaction;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace Editor.Gui.InputUi
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