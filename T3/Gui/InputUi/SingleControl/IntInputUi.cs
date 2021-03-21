using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Interaction;
using T3.Gui.UiHelpers;

namespace T3.Gui.InputUi.SingleControl
{
    public class IntInputUi : SingleControlInputUi<int>
    {
        public override bool IsAnimatable => true;

        public override IInputUi Clone()
        {
            return new IntInputUi()
                       {
                           InputDefinition = InputDefinition,
                           Parent = Parent,
                           PosOnCanvas = PosOnCanvas,
                           Relevancy = Relevancy
                       };
        }

        protected override bool DrawSingleEditControl(string name, ref int value)
        {
            InputEditStateFlags result = InputEditStateFlags.Nothing;
            if (MappedType != null)
            {
                result = DrawEnumInputEdit(ref value, MappedType);
            }
            else
            {
                result = SingleValueEdit.Draw(ref value, new Vector2(-1, 0));
            }

            return (result & InputEditStateFlags.Modified) != 0;
        }

        public  InputEditStateFlags DrawEditControl(ref int value)
        {
            return SingleValueEdit.Draw(ref value, -Vector2.UnitX, -100, 100, false, 0);
        }
        
        
        protected override void DrawAnimatedValue(string name, InputSlot<int> inputSlot, Animator animator)
        {
            double time = EvaluationContext.GlobalTimeInBars;
            var curves = animator.GetCurvesForInput(inputSlot);
            foreach (var curve in curves)
            {
                int value = (int)curve.GetSampledValue(time);
                var editState = DrawEditControl(name, ref value);
                if ((editState & InputEditStateFlags.Modified) == InputEditStateFlags.Modified)
                {
                    // Animated ints are constant by default
                    var key = new VDefinition()
                                        {
                                            InType = VDefinition.Interpolation.Constant,
                                            OutType = VDefinition.Interpolation.Constant,
                                            InEditMode = VDefinition.EditMode.Constant,
                                            OutEditMode = VDefinition.EditMode.Constant,                                            
                                            Value = value,
                                        };
                    
                    curve.AddOrUpdateV(time, key);
                }
            }
        }

        
        public override void ApplyValueToAnimation(IInputSlot inputSlot, InputValue inputValue, Animator animator) 
        {
            if (inputValue is InputValue<int> float3InputValue)
            {
                int value = float3InputValue.Value;
                var curves = animator.GetCurvesForInput(inputSlot).ToArray();
                Curve.UpdateCurveValues(curves, EvaluationContext.GlobalTimeInBars, new [] { (float)value});   
            }
        }
        
        protected override string GetSlotValueAsString(ref int value)
        {
            // This is a stub of value editing. Sadly it's very hard to get
            // under control because of styling issues and because in GraphNodes
            // The op body captures the mouse event first.
            //SingleValueEdit.Draw(ref floatValue,  -Vector2.UnitX);

            return value.ToString();
        }

        protected override void DrawReadOnlyControl(string name, ref int value)
        {
            if (MappedType != null)
            {
                var enumInfo = EnumCache.Instance.GetEnumEntry(MappedType);
                int nameIndex = Array.IndexOf(enumInfo.ValuesAsInt, value);
                ImGui.Text(nameIndex != -1 ? enumInfo.ValueNames[nameIndex] : "Invalid enum value.");
            }
            else
            {
                ImGui.InputInt(name, ref value, 0, 0, ImGuiInputTextFlags.ReadOnly);
            }
        }

        public static InputEditStateFlags DrawEnumInputEdit(ref int value, Type enumType)
        {
            var enumInfo = EnumCache.Instance.GetEnumEntry(enumType);

            if (enumInfo.IsFlagEnum)
            {
                // show as checkboxes
                InputEditStateFlags editStateFlags = InputEditStateFlags.Nothing;
                if (ImGui.TreeNode("##enumParamInt124"))
                {
                    bool[] checks = enumInfo.SetFlags;
                    for (int i = 0; i < enumInfo.ValueNames.Length; i++)
                    {
                        int enumValueAsInt = enumInfo.ValuesAsInt[i];
                        checks[i] = (value & enumValueAsInt) > 0;
                        if (ImGui.Checkbox(enumInfo.ValueNames[i], ref checks[i]))
                        {
                            // value modified, store new flag
                            if (checks[i])
                            {
                                value |= enumValueAsInt;
                            }
                            else
                            {
                                value &= ~enumValueAsInt;
                            }

                            editStateFlags |= InputEditStateFlags.Modified;
                        }

                        if (ImGui.IsItemClicked())
                        {
                            editStateFlags |= InputEditStateFlags.Started;
                        }

                        if (ImGui.IsItemDeactivatedAfterEdit())
                        {
                            editStateFlags |= InputEditStateFlags.Finished;
                        }
                    }

                    ImGui.TreePop();
                }

                return editStateFlags;
            }
            else
            {
                int index = Array.IndexOf(enumInfo.ValuesAsInt, value);
                InputEditStateFlags editStateFlags = InputEditStateFlags.Nothing;
                bool modified = ImGui.Combo("##dropDownParam", ref index, enumInfo.ValueNames, enumInfo.ValueNames.Length);
                if (modified)
                {
                    value = enumInfo.ValuesAsInt[index];
                    editStateFlags |= InputEditStateFlags.ModifiedAndFinished;
                }

                if (ImGui.IsItemClicked())
                {
                    editStateFlags |= InputEditStateFlags.Started;
                }

                return editStateFlags;
            }
        }
    }
}