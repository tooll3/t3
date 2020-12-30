using System.Numerics;
using System.Reflection;
using ImGuiNET;
using T3.Core.DataTypes;
using T3.Gui.Styling;

namespace T3.Gui.TableView
{
    public static class TableList
    {
        public static bool Draw(StructuredList list)
        {
            return Draw(list, Vector2.Zero);
        }
        
        public static bool Draw(StructuredList list, Vector2 size)
        {
            ImGui.BeginChild("child", size);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2,2));
            const float valueColumnWidth = 60;
            const float lineNumberWidth = 50;
            const float headerHeight = 30;
            var listModified = false;
            ImGui.PushFont(Fonts.FontSmall);
            {
                FieldInfo[] members = list.Type.GetFields();

                ImGui.AlignTextToFramePadding();
                ImGui.Text($"[{list.NumElements}]");
                ImGui.SameLine(lineNumberWidth);
                
                // List Header 
                foreach (var fi in members)
                {
                    if (fi.FieldType == typeof(float))
                    {
                        ImGui.Selectable(" " + fi.Name, false, ImGuiSelectableFlags.None, new Vector2(valueColumnWidth, headerHeight));
                    }
                    else if (fi.FieldType == typeof(string))
                    {
                        ImGui.Selectable(" " + fi.Name, false, ImGuiSelectableFlags.None, new Vector2(valueColumnWidth, headerHeight));
                    }
                    else if (fi.FieldType == typeof(Vector4))
                    {
                        bool isFirst = true;
                        foreach (var c in new[] { ".x", ".y", ".z", ".w" })
                        {
                            ImGui.Selectable((isFirst ? " " + fi.Name : "_") + "\n" + c, false, ImGuiSelectableFlags.None, new Vector2(valueColumnWidth, headerHeight));
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
                                                            cursorScreenPos + new Vector2(1000, ImGui.GetFrameHeight()));

                    if (!isLineVisible)
                    {
                        ImGui.Dummy(new Vector2(1, ImGui.GetFrameHeight()));
                        continue;
                    }

                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(objectIndex+".");
                    ImGui.SameLine(lineNumberWidth);

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
                        else if (fi.FieldType == typeof(string))
                        {
                            if(!(o is string s))
                                s = string.Empty;
                            
                            ImGui.PushID(fi.Name);
                            ImGui.SetNextItemWidth(valueColumnWidth);
                            if (ImGui.InputText("##sdf", ref s, 256))
                            {
                                fi.SetValue(obj, s);
                                objModified = true;
                            }
                            
                            ImGui.SameLine();
                            ImGui.PopID();
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
                        listModified = true;
                    }

                    if (ImGui.Button("+"))
                    {
                        list.Insert(objectIndex, obj);
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("-"))
                    {
                        list.Remove(objectIndex);
                    }
                    
                    ImGui.PopID();
                }
            }
            ImGui.PopFont();
            ImGui.PopStyleVar();
            ImGui.EndChild();
            return listModified;

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