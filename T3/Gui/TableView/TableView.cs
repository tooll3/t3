using System.Numerics;
using System.Reflection;
using ImGuiNET;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Gui.Styling;

namespace T3.Gui.TableView
{
    public static class TableList
    {
        public static bool Draw(StructuredList list)
        {
            const float width = 60;
            var modified = false;
            ImGui.PushFont(Fonts.FontSmall);
            {
                FieldInfo[] members = list.Type.GetFields();

                // List Header 
                foreach (var fi in members)
                {
                    if (fi.FieldType == typeof(float))
                    {
                        ImGui.Selectable(" " + fi.Name, false, ImGuiSelectableFlags.None, new Vector2(width, 30));
                    }
                    else if (fi.FieldType == typeof(Vector4))
                    {
                        bool isFirst = true;
                        foreach (var c in new[] { ".x", ".y", ".z", ".w" })
                        {
                            ImGui.Selectable((isFirst ? " " + fi.Name : "_") + "\n" + c, false, ImGuiSelectableFlags.None, new Vector2(width, 30));
                            ImGui.SameLine();
                            isFirst = false;
                        }
                    }

                    ImGui.SameLine();
                }

                ImGui.NewLine();

                // Values
                for (var objectIndex = 0; objectIndex < list.NumElements; objectIndex++)
                {
                    ImGui.PushID(objectIndex);
                    var obj = list[objectIndex];

                    for (var fieldIndex = 0; fieldIndex < members.Length; fieldIndex++)
                    {
                        FieldInfo fi = members[fieldIndex];
                        var o = fi.GetValue(obj);
                        if (o is float f)
                        {
                            DrawFloatManipulation(ref f, fieldIndex);
                            fi.SetValue(obj, f);
                        }
                        else if (o is Vector4 vector4)
                        {
                            DrawFloatManipulation(ref vector4.X, fieldIndex * 100 + 0);
                            DrawFloatManipulation(ref vector4.Y, fieldIndex * 100 + 1);
                            DrawFloatManipulation(ref vector4.Z, fieldIndex * 100 + 2);
                            DrawFloatManipulation(ref vector4.W, fieldIndex * 100 + 3);
                        }
                        else
                        {
                            ImGui.SetNextItemWidth(width);
                            ImGui.Text("?");
                            ImGui.SameLine();
                        }
                    }

                    list[objectIndex] = obj;

                    ImGui.NewLine();
                    ImGui.PopID();
                }
            }
            ImGui.PopFont();
            return modified;

            void DrawFloatManipulation(ref float f, int index = 0)
            {
                ImGui.PushID(index);
                ImGui.SetNextItemWidth(width);
                if (ImGui.DragFloat("##sdf", ref f))
                {
                    Log.Debug("Changed");
                }
                ImGui.SameLine();
                ImGui.PopID();
            }
        }
    }
}