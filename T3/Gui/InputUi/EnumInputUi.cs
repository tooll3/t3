using System;
using System.Linq;
using System.Reflection;
using ImGuiNET;

namespace T3.Gui.InputUi
{
    public class EnumInputUi<T> : InputValueUi<T> where T : Enum
    {
        protected override InputEditState DrawEditControl(string name, ref T value)
        {
            // todo: check perf impact of creating the list here again and again! -> cache lists
            Type enumType = typeof(T);
            var values = Enum.GetValues(enumType);
            var valueNames = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                valueNames[i] = Enum.GetName(typeof(T), values.GetValue(i));
            }

            if (enumType.GetCustomAttributes<FlagsAttribute>().Any())
            {
                // show as checkboxes
                InputEditState editState = InputEditState.Nothing;
                if (ImGui.TreeNode("##enumParam"))
                {
                    // todo: refactor crappy code below, works but ugly!
                    bool[] checks = new bool[values.Length];
                    int intValue = (int)(object)value;
                    for (int i = 0; i < valueNames.Length; i++)
                    {
                        int enumValueAsInt = (int)values.GetValue(i);
                        checks[i] = (intValue & enumValueAsInt) > 0;
                        if (ImGui.Checkbox(valueNames[i], ref checks[i]))
                        {
                            // value modified, store new flag
                            if (checks[i])
                            {
                                intValue |= enumValueAsInt;
                            }
                            else
                            {
                                intValue &= ~enumValueAsInt;
                            }

                            value = (T)(object)intValue;
                            editState |= InputEditState.Modified;
                        }

                        if (ImGui.IsItemClicked())
                        {
                            editState |= InputEditState.Focused;
                        }

                        if (ImGui.IsItemDeactivatedAfterEdit())
                        {
                            editState |= InputEditState.Finished;
                        }
                    }

                    ImGui.TreePop();
                }

                return editState;
            }
            else
            {
                int index = 0;
                for (int i = 0; i < values.Length; i++)
                {
                    if (values.GetValue(i).Equals(value))
                    {
                        index = i;
                        break;
                    }
                }
                InputEditState editState = InputEditState.Nothing;
                bool modified = ImGui.Combo("##dropDownParam", ref index, valueNames, valueNames.Length);
                if (modified)
                {
                    value = (T)values.GetValue(index);
                    editState |= InputEditState.ModifiedAndFinished;
                }

                if (ImGui.IsItemClicked())
                {
                    editState |= InputEditState.Focused;
                }

                return editState;
            }
        }

        protected override void DrawValueDisplay(string name, ref T value)
        {
            ImGui.Text(value.ToString());
        }
    }
}