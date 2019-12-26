using System;
using System.Linq;
using System.Reflection;
using ImGuiNET;

namespace T3.Gui.InputUi
{
    public class EnumInputUi<T> : InputValueUi<T> where T : Enum
    {
        protected override InputEditStateFlags DrawEditControl(string name, ref T value)
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
                InputEditStateFlags editStateFlags = InputEditStateFlags.Nothing;
                if (ImGui.TreeNode("##enumParam124"))
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
                int index = 0;
                for (int i = 0; i < values.Length; i++)
                {
                    if (values.GetValue(i).Equals(value))
                    {
                        index = i;
                        break;
                    }
                }
                InputEditStateFlags editStateFlags = InputEditStateFlags.Nothing;
                bool modified = ImGui.Combo("##dropDownParam", ref index, valueNames, valueNames.Length);
                if (modified)
                {
                    value = (T)values.GetValue(index);
                    editStateFlags |= InputEditStateFlags.ModifiedAndFinished;
                }

                if (ImGui.IsItemClicked())
                {
                    editStateFlags |= InputEditStateFlags.Started;
                }

                return editStateFlags;
            }
        }

        protected override void DrawReadOnlyControl(string name, ref T value)
        {
            ImGui.Text(value.ToString());
        }
    }
}