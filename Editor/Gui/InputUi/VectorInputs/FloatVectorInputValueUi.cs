using System;
using System.Linq;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.InputUi.VectorInputs
{
    public abstract class FloatVectorInputValueUi<T> : InputValueUi<T>
    {
        protected FloatVectorInputValueUi(int componentCount)
        {
            FloatComponents = new float[componentCount];
        }
        
        public override bool IsAnimatable => true;
        
        protected FloatVectorInputValueUi<T> CloneWithType<TT>() where TT : FloatVectorInputValueUi<T>, new() {
            return new TT() {
                                    Max = Max,
                                    Min = Min,
                                    _scale = _scale,
                                    Clamp = Clamp,
                                    InputDefinition = InputDefinition,
                                    Parent = Parent,
                                    PosOnCanvas = PosOnCanvas,
                                    Relevancy = Relevancy,
                                    Size = Size,
                                };
        }
        
        protected override InputEditStateFlags DrawAnimatedValue(string name, InputSlot<T> inputSlot, Animator animator)
        {
            var time = Playback.Current.TimeInBars;
            var curves = animator.GetCurvesForInput(inputSlot).ToArray();
            if (curves.Length < FloatComponents.Length)
            {
                ImGui.PushID(inputSlot.Parent.SymbolChildId.GetHashCode() + inputSlot.Id.GetHashCode());
                DrawReadOnlyControl(name, ref inputSlot.Value);
                ImGui.PopID();
                return InputEditStateFlags.Nothing; 
            }

            for (var index = 0; index < FloatComponents.Length; index++)
            {
                FloatComponents[index] = (float)curves[index].GetSampledValue(time);
            }
            
            ImGui.PushID(inputSlot.Parent.SymbolChildId.GetHashCode() + inputSlot.Id.GetHashCode());
            var inputEditState = DrawEditControl(name, ref inputSlot.Value);
            ImGui.PopID();
            
            if ((inputEditState & InputEditStateFlags.Modified) == InputEditStateFlags.Modified)
            {
                inputSlot.SetTypedInputValue(inputSlot.Value);
            }
            return inputEditState;
        }
        
        
        
        protected override void DrawReadOnlyControl(string name, ref T float2Value)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Color.Blue.Rgba);
            DrawEditControl(name, ref float2Value);
            ImGui.PopStyleColor();
        }
        
        protected override string GetSlotValueAsString(ref T float2Value)
        {
            return string.Format(T3Ui.FloatNumberFormat, float2Value);
        }


        public abstract override void ApplyValueToAnimation(IInputSlot inputSlot, InputValue inputValue, Animator animator, double time);
        
        public override void DrawSettings()
        {
            base.DrawSettings();

            FormInputs.AddFloat("Scale", ref _scale, 0.1f,0,100, false, "This will affect how responsive value ladder or jog dial are in Parameter Window. Use 0 to derived scale from Min/Max range.", 0f);
            FormInputs.AddFloat("Min", ref Min, float.NegativeInfinity, float.PositiveInfinity, 0.1f, false, "Set to range to defined a visible slider bar in parameter window", float.NegativeInfinity);
            FormInputs.AddFloat("Max", ref Max, float.NegativeInfinity, float.PositiveInfinity, 0.1f, false, "Set to range to defined a visible slider bar in parameter window", float.PositiveInfinity);
            FormInputs.AddCheckBox("Clamp slider to range", ref Clamp, "This will only clamp slider. Users are still able to enter numerical values outside of range.");
            FormInputs.AddStringInput("Custom Format", ref Format, "Custom format like {0:0.0}", null, "Defines custom value format. Here are some examples:\n\n{0:0.00000} - High precision\n{0:0}× - With a suffix");
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
            
            if(Clamp)
                writer.WriteValue("Clamp", Clamp);
            
            if(!string.IsNullOrEmpty(Format))
                writer.WriteObject("Format", Format);

            // ReSharper enable CompareOfFloatsByEqualityOperator
        }

        public override void Read(JToken inputToken)
        {
            base.Read(inputToken);

            Min = inputToken["Min"]?.Value<float>() ?? DefaultMin;
            Max = inputToken["Max"]?.Value<float>() ?? DefaultMax;
            _scale = inputToken["Scale"]?.Value<float>() ?? DefaultScale;
            Clamp = inputToken["Clamp"]?.Value<bool>() ?? false;
            Format = inputToken["Format"]?.Value<string>();
        }

        private static float GetScaleFromRange(float scale, float min, float max)
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
        
        public float Min  = DefaultMin;
        public float Max  = DefaultMax;
        private float _scale = DefaultScale;
        public float Scale  => FloatInputUi.GetScaleFromRange(_scale, Min, Max);
        public bool Clamp;
        public string Format;
        
        protected readonly float[] FloatComponents;

        private const float DefaultScale = 0.0f;
        private const float DefaultMin = -9999999f;
        private const float DefaultMax = 9999999f;        
    }
}