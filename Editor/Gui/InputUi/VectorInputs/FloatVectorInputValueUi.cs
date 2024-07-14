using System;
using System.Linq;
using System.Numerics;
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

        protected FloatVectorInputValueUi<T> CloneWithType<TT>() where TT : FloatVectorInputValueUi<T>, new()
        {
            return new TT()
                       {
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
            var inputEditState = DrawEditControl(name, inputSlot.Input, ref inputSlot.Value, false);
            ImGui.PopID();

            if ((inputEditState & InputEditStateFlags.Modified) == InputEditStateFlags.Modified)
            {
                inputSlot.SetTypedInputValue(inputSlot.Value);
            }

            return inputEditState;
        }

        protected override void DrawReadOnlyControl(string name, ref T float2Value)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.StatusAutomated.Rgba);
            DrawEditControl(name, null, ref float2Value, true);
            ImGui.PopStyleColor();
        }

        protected override string GetSlotValueAsString(ref T float2Value)
        {
            return string.Format(T3Ui.FloatNumberFormat, float2Value);
        }

        public abstract override void ApplyValueToAnimation(IInputSlot inputSlot, InputValue inputValue, Animator animator, double time);

        private struct ValueSettingPreset
        {
            public string Title;
            public float Scale;
            public float MinValue;
            public float MaxValue;
            public string CustomFormat;
            public bool ClampRange;
        }

        private ValueSettingPreset[] _valueSettingPresets =
            {
                new()
                    {
                        Scale = 0,
                        Title = "Rotation",
                        MinValue = -180,
                        MaxValue = 180,
                        CustomFormat = "{0:0.0}°"
                    },
                new()
                    {
                        Scale = 0,
                        Title = "Translation",
                        MinValue = -2,
                        MaxValue = 2,
                        CustomFormat = null
                    },
                new()
                    {
                        Scale = 0,
                        Title = "Color",
                        MinValue = 0,
                        MaxValue = 1,
                        CustomFormat = null,
                        ClampRange = true,
                    },
                new()
                    {
                        Scale = 0,
                        Title = "Signed Scale × Factor",
                        MinValue = -5,
                        MaxValue = 5,
                        CustomFormat = "{0:0.000} ×",
                        ClampRange = false,
                    },
                new()
                    {
                        Scale = 0,
                        Title = "Scale × Factor",
                        MinValue = 0,
                        MaxValue = 5,
                        CustomFormat = "{0:0.000} ×",
                        ClampRange = false,
                    },
                new()
                    {
                        Scale = 0,
                        Title = "Bias&Gain",
                        MinValue = 0,
                        MaxValue = 1,
                        CustomFormat = "{0:0.000}",
                        ClampRange = true,
                    },                
            };

        public override void DrawSettings()
        {
            var keepPosition = ImGui.GetCursorPos();
            ImGui.SetCursorPos(new Vector2(ImGui.GetWindowContentRegionMax().X - ImGui.GetFrameHeight() - ImGui.GetStyle().FramePadding.X, 
                                           ImGui.GetWindowContentRegionMin().Y));
            
            if (CustomComponents.IconButton(Icon.Settings2, Vector2.One * ImGui.GetFrameHeight()))
            {
                ImGui.OpenPopup("customFormats");
            }
            ImGui.SetCursorPos(keepPosition);

            if (ImGui.BeginPopup("customFormats", ImGuiWindowFlags.Popup))
            {
                ImGui.TextUnformatted("Apply formatting presets...");
                foreach (var p in _valueSettingPresets)
                {
                    if (ImGui.Button(p.Title))
                    {
                        Min = p.MinValue;
                        Max = p.MaxValue;
                        _scale = p.Scale;
                        Clamp = p.ClampRange;
                        Format = p.CustomFormat;
                        Parent.FlagAsModified();
                        ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.EndPopup();
            }
            
            base.DrawSettings();
            
            FormInputs.DrawFieldSetHeader("Value Range");
            if (FormInputs.DrawValueRangeControl(ref Min, ref Max, ref _scale,  ref Clamp, DefaultMin, DefaultMax, DefaultScale))
            {
                Parent.FlagAsModified();
            }
            
            FormInputs.DrawFieldSetHeader("Custom Value format");
            
            if (
                FormInputs.AddStringInput(null, 
                                          ref Format,
                                          "Custom format like {0:0.0}",
                                          null,
                                          "Defines custom value format. Here are some examples:\n\n"+
                                          "{0:0.00000} - High precision\n"+
                                          "{0:0}× - With a suffix\n" +
                                          "{0:G5} - scientific notation", 
                                          null)
                )
            {
                if (string.IsNullOrWhiteSpace(Format))
                {
                    Format = null;
                }
            }
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

            if (Clamp)
                writer.WriteValue("Clamp", Clamp);

            if (!string.IsNullOrEmpty(Format))
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

        public float Min = DefaultMin;
        public float Max = DefaultMax;
        private float _scale = DefaultScale;
        public float Scale => FloatInputUi.GetScaleFromRange(_scale, Min, Max);
        public bool Clamp;
        public string Format;

        protected readonly float[] FloatComponents;

        private const float DefaultScale = 0.0f;
        private const float DefaultMin = -9999999f;
        private const float DefaultMax = 9999999f;
    }
}