using System;
using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Interaction;

namespace T3.Gui.InputUi
{
    public class FloatInputUi : InputValueUi<float>
    {
        public override bool IsAnimatable => true;
        public override bool IsVariable => true;

        public override IInputUi Clone()
        {
            return new FloatInputUi()
                       {
                           Max = Max,
                           Min = Min,
                           _scale = _scale,
                           Clamp = Clamp,
                           InputDefinition = InputDefinition,
                           Parent = Parent,
                           PosOnCanvas = PosOnCanvas,
                           Relevancy = Relevancy,
                           Size = Size
                       };
        }

        protected override InputEditStateFlags DrawEditControl(string name, ref float value)
        {
            ImGui.PushID(Id.GetHashCode());
            var inputEditState = SingleValueEdit.Draw(ref value, -Vector2.UnitX, Min, Max, Clamp, Scale);
            ImGui.PopID();
            return inputEditState;
        }

        public InputEditStateFlags DrawEditControl(ref float value)
        {
            return SingleValueEdit.Draw(ref value, -Vector2.UnitX, Min, Max, Clamp, Scale);
        }

        protected override void DrawReadOnlyControl(string name, ref float value)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Color.Blue.Rgba);
            DrawEditControl(name, ref value);
            ImGui.PopStyleColor();
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
            double time = EvaluationContext.GlobalTimeInBars;
            var curves = animator.GetCurvesForInput(inputSlot);
            foreach (var curve in curves)
            {
                float value = (float)curve.GetSampledValue(time);
                var editState = DrawEditControl(name, ref value);
                if ((editState & InputEditStateFlags.Modified) == InputEditStateFlags.Modified)
                {
                    var previousU = curve.GetPreviousU(time);

                    var key = (previousU != null)
                                  ? curve.GetV(previousU.Value).Clone()
                                  : new VDefinition();

                    key.Value = value;
                    curve.AddOrUpdateV(time, key);
                }
            }
        }

        public override void ApplyValueToAnimation(IInputSlot inputSlot, InputValue inputValue, Animator animator)
        {
            if (inputValue is InputValue<float> floatInputValue)
            {
                float value = floatInputValue.Value;

                double time = EvaluationContext.GlobalTimeInBars;
                var curves = animator.GetCurvesForInput(inputSlot);
                foreach (var curve in curves)
                {
                    var key = curve.GetV(time) ?? new VDefinition { U = time };
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
            ImGui.DragFloat("Scale", ref _scale);
            ImGui.Checkbox("Clamp Range", ref Clamp);
        }

        public override void Write(JsonTextWriter writer)
        {
            base.Write(writer);

            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (Min != DefaultMin)
                writer.WriteValue("Min", Min);

            if (Max != DefaultMax)
                writer.WriteValue("Max", Max);

            if (_scale != DefaultScale)
                writer.WriteValue("Scale", _scale);

            if (Clamp != false)
                writer.WriteValue("Clamp", Clamp);
            // ReSharper enable CompareOfFloatsByEqualityOperator
        }

        public override void Read(JToken inputToken)
        {
            base.Read(inputToken);

            Min = inputToken["Min"]?.Value<float>() ?? DefaultMin;
            Max = inputToken["Max"]?.Value<float>() ?? DefaultMax;
            _scale = inputToken["Scale"]?.Value<float>() ?? DefaultScale;
            Clamp = inputToken["Clamp"]?.Value<bool>() ?? false;
        }

        public float Min = DefaultMin;
        public float Max = DefaultMax;
        private float _scale = DefaultScale;
        public float Scale => GetScaleFromRange(_scale, Min, Max);

        public bool Clamp = false;

        private const float DefaultScale = 0.0f;
        private const float DefaultMin = -9999999f;
        private const float DefaultMax = 9999999f;

        public static float GetScaleFromRange(float scale, float min, float max)
        {
            // Automatically set scale from range
            if (scale > 0)
                return scale;

            // A lame hack because we can't set infinity values in parameter properties
            if (min < -9999 || max > 9999)
            {
                return 0.01f;
            }

            return Math.Abs(min - max) / 100;
        }
    }
}