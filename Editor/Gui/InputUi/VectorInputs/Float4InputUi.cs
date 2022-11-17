using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Interaction;

namespace T3.Editor.Gui.InputUi.VectorInputs
{
    public class Float4InputUi : FloatVectorInputValueUi<Vector4>
    {
        public Float4InputUi() : base(4)
        {
            Min = 0;
            Max = 1;
            Clamp = true;
        }
        
        public override IInputUi Clone()
        {
            return CloneWithType<Float4InputUi>();
        }

        public override void ApplyValueToAnimation(IInputSlot inputSlot, InputValue inputValue, Animator animator, double time)
        {
            if (inputValue is not InputValue<Vector4> typedInputValue)
                return;
            
            var curves = animator.GetCurvesForInput(inputSlot).ToArray();
            typedInputValue.Value.CopyTo(FloatComponents);
            Curve.UpdateCurveValues(curves, time, FloatComponents);
        }

        protected override InputEditStateFlags DrawEditControl(string name, ref Vector4 float4Value)
        {
            float4Value.CopyTo(FloatComponents);
            var thumbWidth = ImGui.GetFrameHeight();
            var inputEditState = VectorValueEdit.Draw(FloatComponents, Min, Max, Scale, Clamp, thumbWidth);

            ImGui.SameLine();
            float4Value = new Vector4(FloatComponents[0], 
                                      FloatComponents[1],
                                      FloatComponents[2],
                                      FloatComponents[3]);

            var result = ColorEditButton.Draw(ref float4Value, Vector2.Zero); 
            if (result != InputEditStateFlags.Nothing)
            {
                float4Value.CopyTo(FloatComponents);
                inputEditState |= result;
            }
            return inputEditState;
        }
    }
}