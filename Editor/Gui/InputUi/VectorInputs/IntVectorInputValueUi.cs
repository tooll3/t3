using System;
using System.Linq;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace Editor.Gui.InputUi
{
    public abstract class IntVectorInputValueUi<T> : InputValueUi<T>
    {
        protected IntVectorInputValueUi(int componentCount)
        {
            IntComponents = new int[componentCount];
        }
        
        public override bool IsAnimatable => true;
        
        protected IntVectorInputValueUi<T> CloneWithType<TT>() where TT : IntVectorInputValueUi<T>, new() {
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
            if (curves.Length < IntComponents.Length)
            {
                ImGui.PushID(inputSlot.Parent.SymbolChildId.GetHashCode() + inputSlot.Id.GetHashCode());
                DrawReadOnlyControl(name, ref inputSlot.Value);
                ImGui.PopID();
                return InputEditStateFlags.Nothing; 
            }

            for (var index = 0; index < IntComponents.Length; index++)
            {
                IntComponents[index] = (int)(curves[index].GetSampledValue(time) + 0.5f);
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
        
        
        
        protected override void DrawReadOnlyControl(string name, ref T int2Value)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Color.Blue.Rgba);
            DrawEditControl(name, ref int2Value);
            ImGui.PopStyleColor();
        }
        
        protected override string GetSlotValueAsString(ref T int2Value)
        {
            return string.Format(T3Ui.FloatNumberFormat, int2Value);
        }


        public abstract override void ApplyValueToAnimation(IInputSlot inputSlot, InputValue inputValue, Animator animator, double time);
        
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
            
            if(Clamp)
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
        
        
        public int Min  = DefaultMin;
        public int Max  = DefaultMax;
        private float _scale = DefaultScale;
        public float Scale  => 0.1f;
        public bool Clamp;
        
        protected readonly int[] IntComponents;

        private const float DefaultScale = 0.1f;
        private const int DefaultMin = int.MinValue;
        private const int DefaultMax = int.MaxValue;        
    }
}