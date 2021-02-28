using System.Linq;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Interaction;

namespace T3.Gui.InputUi.SingleControl
{
    public class Size2InputUi : InputValueUi<Size2>
    {
        public override bool IsAnimatable => true;

        public override IInputUi Clone()
        {
            return new Size2InputUi()
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

        protected override InputEditStateFlags DrawEditControl(string name, ref Size2 value)
        {
            Components[0] = value.Width;
            Components[1] = value.Height;
            var inputEditState = VectorValueEdit.Draw(Components, Min, Max, _scale, Clamp);
            value = new Size2(Components[0], Components[1]);
            return inputEditState;
        }

        protected override void DrawReadOnlyControl(string name, ref Size2 value)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Color.Blue.Rgba);
            DrawEditControl(name, ref value);
            ImGui.PopStyleColor();
        }

        protected override void DrawAnimatedValue(string name, InputSlot<Size2> inputSlot, Animator animator)
        {
            double time = EvaluationContext.GlobalTimeInBars;
            var curves = animator.GetCurvesForInput(inputSlot).ToArray();

            if (curves.Length < Components.Length)
            {
                DrawReadOnlyControl(name, ref inputSlot.Value);
                return;
            }

            for (var index = 0; index < Components.Length; index++)
            {
                Components[index] = (int)curves[index].GetSampledValue(time);
            }

            var inputEditState = VectorValueEdit.Draw(Components, Min, Max, _scale, Clamp);
            if (inputEditState == InputEditStateFlags.Nothing)
                return;

            for (var index = 0; index < Components.Length; index++)
            {
                var key = curves[index].GetV(time) ?? new VDefinition
                                                          {
                                                              InType = VDefinition.Interpolation.Constant,
                                                              OutType = VDefinition.Interpolation.Constant,
                                                              InEditMode = VDefinition.EditMode.Constant,
                                                              OutEditMode = VDefinition.EditMode.Constant,
                                                              U = time
                                                          };
                key.Value = Components[index];
                curves[index].AddOrUpdateV(time, key);
            }
        }

        public override void DrawSettings()
        {
            base.DrawSettings();

            ImGui.DragInt("Min", ref Min);
            ImGui.DragInt("Max", ref Max);
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

            Min = inputToken["Min"]?.Value<int>() ?? DefaultMin;
            Max = inputToken["Max"]?.Value<int>() ?? DefaultMax;
            _scale = inputToken["Scale"]?.Value<float>() ?? DefaultScale;
            Clamp = inputToken["Clamp"]?.Value<bool>() ?? false;
        }

        public int Min = DefaultMin;
        public int Max = DefaultMax;
        public bool Clamp;
        private static readonly int[] Components = new int[2];
        private float _scale = DefaultScale;

        private const float DefaultScale = 0.0f;
        private const int DefaultMin = int.MinValue;
        private const int DefaultMax = int.MaxValue;
    }
}