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
            ImGui.BeginChild("child");
            const float valueColumnWidth = 60;
            const float lineNumberWidth = 50;
            var modified = false;
            ImGui.PushFont(Fonts.FontSmall);
            {
                FieldInfo[] members = list.Type.GetFields();

                ImGui.SameLine(lineNumberWidth);
                // List Header 
                foreach (var fi in members)
                {
                    if (fi.FieldType == typeof(float))
                    {
                        ImGui.Selectable(" " + fi.Name, false, ImGuiSelectableFlags.None, new Vector2(valueColumnWidth, 30));
                    }
                    else if (fi.FieldType == typeof(Vector4))
                    {
                        bool isFirst = true;
                        foreach (var c in new[] { ".x", ".y", ".z", ".w" })
                        {
                            ImGui.Selectable((isFirst ? " " + fi.Name : "_") + "\n" + c, false, ImGuiSelectableFlags.None, new Vector2(valueColumnWidth, 30));
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
                    var cursorScreenPos = ImGui.GetCursorScreenPos();

                    var isLineVisible = ImGui.IsRectVisible(cursorScreenPos,
                                        cursorScreenPos + new Vector2(1000, 60));

                    if (!isLineVisible)
                    {
                        ImGui.Dummy(new Vector2(1, ImGui.GetFrameHeight()));
                        continue;
                    }
                    ImGui.Text("" + objectIndex);
                    ImGui.SameLine(40);
                    
                    ImGui.PushID(objectIndex);
                    var obj = list[objectIndex];

                    var objModified = false;
                    for (var fieldIndex = 0; fieldIndex < members.Length; fieldIndex++)
                    {
                        FieldInfo fi = members[fieldIndex];
                        var o = fi.GetValue(obj);
                        if (o is float f)
                        {
                            if (DrawFloatManipulation(ref f, fieldIndex))
                            {
                                fi.SetValue(obj, f);
                                objModified = true;
                            }
                        }
                        else if (o is Vector4 vector4)
                        {
                            if (DrawFloatManipulation(ref vector4.X, fieldIndex * 100 + 0)
                                | DrawFloatManipulation(ref vector4.Y, fieldIndex * 100 + 1)
                                | DrawFloatManipulation(ref vector4.Z, fieldIndex * 100 + 2)
                                | DrawFloatManipulation(ref vector4.W, fieldIndex * 100 + 3))
                            {
                                fi.SetValue(obj, vector4);
                                objModified = true;
                            }
                        }
                        else
                        {
                            ImGui.SetNextItemWidth(valueColumnWidth);
                            ImGui.Text("?");
                            ImGui.SameLine();
                        }
                    }

                    if (objModified)
                    {
                        list[objectIndex] = obj;
                    }
                    
                    if(ImGui.Button("+"))
                    {

                    }
                    ImGui.SameLine();

                    if(ImGui.Button("-"))
                    {

                    }
                    ImGui.PopID();
                }
            }
            ImGui.PopFont();
            ImGui.EndChild();
            return modified;

            bool DrawFloatManipulation(ref float f, int index = 0)
            {
                ImGui.PushID(index);
                ImGui.SetNextItemWidth(valueColumnWidth);
                var fieldModified = ImGui.DragFloat("##sdf", ref f);
                ImGui.SameLine();
                ImGui.PopID();
                return fieldModified;
            }
        }
    }
}
