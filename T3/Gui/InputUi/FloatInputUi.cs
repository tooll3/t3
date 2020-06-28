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

        public override IInputUi Clone()
        {
            return new FloatInputUi()
                   {
                       _max = _max,
                       _min = _min,
                       _scale = _scale,
                       _clamp = _clamp,
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
            var inputEditState = SingleValueEdit.Draw(ref value, -Vector2.UnitX, _min, _max, _clamp, _scale);
            ImGui.PopID();
            return inputEditState;
        }

        public  InputEditStateFlags DrawEditControl(ref float value)
        {
            return SingleValueEdit.Draw(ref value, -Vector2.UnitX, _min, _max, _clamp, _scale);
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

        public override void DrawSettings()
        {
            base.DrawSettings();

            ImGui.DragFloat("Min", ref _min);
            ImGui.DragFloat("Max", ref _max);
            ImGui.DragFloat("Scale", ref _scale);
            ImGui.Checkbox("Clamp Range", ref _clamp);
        }

        public override void Write(JsonTextWriter writer)
        {
            base.Write(writer);

            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (_min != DefaultMin)
                writer.WriteValue("Min", _min);

            if (_max != DefaultMax)
                writer.WriteValue("Max", _max);

            if (_scale != DefaultScale)
                writer.WriteValue("Scale", _scale);
            
            if(_clamp != false)
                writer.WriteValue("Clamp", _clamp);
            // ReSharper enable CompareOfFloatsByEqualityOperator
        }

        public override void Read(JToken inputToken)
        {
            base.Read(inputToken);

            _min = inputToken["Min"]?.Value<float>() ?? DefaultMin;
            _max = inputToken["Max"]?.Value<float>() ?? DefaultMax;
            _scale = inputToken["Scale"]?.Value<float>() ?? DefaultScale;
            _clamp = inputToken["Clamp"]?.Value<bool>() ?? false;
        }

        private float _min = DefaultMin;
        private float _max = DefaultMax;
        private float _scale = DefaultScale;
        private bool _clamp = false;

        private const float DefaultScale = 0.0f;
        private const float DefaultMin = -9999999f;
        private const float DefaultMax = 9999999f;
    }
}