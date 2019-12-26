using System.Configuration;
using System.Globalization;
using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Gui.Interaction;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace T3.Gui.InputUi
{
    public class FloatInputUi : InputValueUi<float>
    {
        public override bool IsAnimatable => true;
        private float Min = DefaultMin;
        private float Max = DefaultMax;
        private float Scale = DefaultScale;

        protected override InputEditStateFlags DrawEditControl(string name, ref float value)
        {
            ImGui.PushID(Id.GetHashCode());
            var inputEditState = SingleValueEdit.Draw(ref value, -Vector2.UnitX, Min, Max, Scale);
            ImGui.PopID();
            return inputEditState;
        }

        protected override void DrawReadOnlyControl(string name, ref float value)
        {
            ImGui.InputFloat(name, ref value, step: 0.0f, step_fast: 0.0f, $"%f", flags: ImGuiInputTextFlags.ReadOnly);
        }

        protected override string GetSlotValueAsString(ref float floatValue)
        {
            // This is a stub of value editing. Sadly it's very hard to get
            // under control because of styling issues and because in GraphNodes
            // The op body captures the mouse event first.
            //
            //SingleValueEdit.Draw(ref floatValue,  -Vector2.UnitX);
            
            return string.Format(T3Ui.FloatNumberFormat, floatValue);
        }
        
        protected override void DrawAnimatedValue(string name, InputSlot<float> inputSlot, Animator animator)
        {
            double time = EvaluationContext.GlobalTime;
            var curves = animator.GetCurvesForInput(inputSlot);
            foreach (var curve in curves)
            {
                float value = (float)curve.GetSampledValue(time);
                var editState = DrawEditControl(name, ref value);
                if ((editState & InputEditStateFlags.Modified) == InputEditStateFlags.Modified)
                {
                    var key = curve.GetV(time);
                    if (key == null)
                        key = new VDefinition() { U = time };

                    key.Value = value;
                    curve.AddOrUpdateV(time, key);
                }
            }
        }

        public override void DrawSettings()
        {
            base.DrawSettings();

            ImGui.DragFloat("Min", ref Min);
            ImGui.DragFloat("Max", ref Max);
            ImGui.DragFloat("Scale", ref Scale);
        }

        public override void Write(JsonTextWriter writer)
        {
            base.Write(writer);

            if (Min != DefaultMin)
                writer.WriteValue("Min", Min);

            if (Max != DefaultMax)
                writer.WriteValue("Max", Max);

            if (Scale != DefaultScale)
                writer.WriteValue("Scale", Scale);
        }

        public override void Read(JToken inputToken)
        {
            base.Read(inputToken);

            Min = inputToken["Min"]?.Value<float>() ?? DefaultMin;
            Max = inputToken["Max"]?.Value<float>() ?? DefaultMax;
            Scale = inputToken["Scale"]?.Value<float>() ?? DefaultScale;
        }

        private const float DefaultScale = 0.01f;
        private const float DefaultMin = -9999999f;
        private const float DefaultMax = 9999999f;
    }
}