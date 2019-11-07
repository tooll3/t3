using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Operator;

namespace T3.Gui.InputUi
{
    public class FloatInputUi : SingleControlInputUi<float>
    {
        public override bool IsAnimatable => true;
        public float Min = -100.0f;
        public float Max = 100.0f;

        public override bool DrawSingleEditControl(string name, ref float value)
        {
            return ImGui.DragFloat("##floatEdit", ref value, 0.0f, Min, Max);
        }

        protected override void DrawValueDisplay(string name, ref float value)
        {
            ImGui.InputFloat(name, ref value, 0.0f, 0.0f, "%f", ImGuiInputTextFlags.ReadOnly);
        }

        protected override void DrawAnimatedValue(string name, InputSlot<float> inputSlot, Animator animator)
        {
            double time = EvaluationContext.GlobalTime;
            var curves = animator.GetCurvesForInput(inputSlot);
            foreach (var curve in curves)
            {
                float value = (float)curve.GetSampledValue(time);
                var editState = DrawEditControl(name, ref value);
                if ((editState & InputEditState.Modified) == InputEditState.Modified)
                {
                    var key = curve.GetV(time);
                    if (key == null)
                        key = new VDefinition() { U = time };
                    key.Value = value;
                    curve.AddOrUpdateV(time, key);
                }
            }
        }

        public override void DrawParameterEdits()
        {
            base.DrawParameterEdits();

            ImGui.DragFloat("Min", ref Min);
            ImGui.DragFloat("Max", ref Max);
        }

        public override void Write(JsonTextWriter writer)
        {
            base.Write(writer);

            writer.WriteValue("Min", Min);
            writer.WriteValue("Max", Max);
        }

        public override void Read(JToken inputToken)
        {
            base.Read(inputToken);

            Min = inputToken["Min"].Value<float>();
            Max = inputToken["Max"].Value<float>();
        }
    }
}