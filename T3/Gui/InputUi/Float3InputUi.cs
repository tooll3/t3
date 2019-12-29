using System.Linq;
using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Gui.Interaction;


namespace T3.Gui.InputUi
{
    public class Float3InputUi : InputValueUi<Vector3>
    {
        public override bool IsAnimatable => true;

        protected override InputEditStateFlags DrawEditControl(string name, ref Vector3 float3Value)
        {
            float3Value.CopyTo(_components);
            var inputEditState = VectorValueEdit.Draw(_components, _min, _max, _scale);
            float3Value = new Vector3(_components[0], _components[1], _components[2]);

            return inputEditState;
        }

        private static float[] _components = new float[3];

        protected override void DrawReadOnlyControl(string name, ref Vector3 float3Value)
        {
            ImGui.InputFloat3(name, ref float3Value, $"%f", flags: ImGuiInputTextFlags.ReadOnly);
        }

        protected override string GetSlotValueAsString(ref Vector3 float3Value)
        {
            // This is a stub of value editing. Sadly it's very hard to get
            // under control because of styling issues and because in GraphNodes
            // The op body captures the mouse event first.
            //
            //SingleValueEdit.Draw(ref floatValue,  -Vector2.UnitX);

            return string.Format(T3Ui.FloatNumberFormat, float3Value);
        }

        protected override void DrawAnimatedValue(string name, InputSlot<Vector3> inputSlot, Animator animator)
        {
            double time = EvaluationContext.GlobalTime;
            var curves = animator.GetCurvesForInput(inputSlot).ToArray();
            if (curves.Length < _components.Length)
            {
                DrawReadOnlyControl(name, ref inputSlot.Value);
                return;
            }

            for (var index = 0; index < _components.Length; index++)
            {
                _components[index] = (float)curves[index].GetSampledValue(time);
            }

            var inputEditState = VectorValueEdit.Draw(_components, _min, _max, _scale);
            if (inputEditState == InputEditStateFlags.Nothing)
                return;

            for (var index = 0; index < _components.Length; index++)
            {
                var key = curves[index].GetV(time) ?? new VDefinition { U = time };
                key.Value = _components[index];
                curves[index].AddOrUpdateV(time, key);
            }
        }

        public override void DrawSettings()
        {
            base.DrawSettings();

            ImGui.DragFloat("Min", ref _min);
            ImGui.DragFloat("Max", ref _max);
            ImGui.DragFloat("Scale", ref _scale);
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
            // ReSharper enable CompareOfFloatsByEqualityOperator
        }

        public override void Read(JToken inputToken)
        {
            base.Read(inputToken);

            _min = inputToken["Min"]?.Value<float>() ?? DefaultMin;
            _max = inputToken["Max"]?.Value<float>() ?? DefaultMax;
            _scale = inputToken["Scale"]?.Value<float>() ?? DefaultScale;
        }

        private float _min = DefaultMin;
        private float _max = DefaultMax;
        private float _scale = DefaultScale;

        private const float DefaultScale = 0.01f;
        private const float DefaultMin = -9999999f;
        private const float DefaultMax = 9999999f;
    }
}