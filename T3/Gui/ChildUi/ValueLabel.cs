using System;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.Operator.Slots;
using T3.Gui.Interaction;
using T3.Gui.Styling;
using UiHelpers;

namespace T3.Gui.ChildUi
{
    public static class ValueLabel
    {
        public static bool Draw(ImDrawListPtr drawList, ImRect screenRect, Vector2 alignment, InputSlot<float> remapValue, Color color)
        {
            var modified = false;
            var valueText = $"{remapValue.Value:G5}";
            var hashCode = remapValue.GetHashCode();
            ImGui.PushID(hashCode);

            // Draw aligned label
            {
                ImGui.PushFont(Fonts.FontSmall);
                var labelSize = ImGui.CalcTextSize(valueText);
                var space = screenRect.GetSize() - labelSize;
                var position = screenRect.Min + space * alignment;
                drawList.AddText(MathUtils.Floor(position), color, valueText);
                ImGui.PopFont();
            }

            // InputGizmo
            {
                var labelSize = screenRect.GetSize() / 2;
                var space = screenRect.GetSize() - labelSize;
                var position = screenRect.Min + space * alignment;
                ImGui.SetCursorScreenPos(position);
                if (ImGui.GetIO().KeyCtrl || _jogDialValue != null)
                {
                    ImGui.InvisibleButton("button", labelSize);
                    double value = remapValue.TypedInputValue.Value;
                    if (ImGui.IsItemActivated() && ImGui.GetIO().KeyCtrl)
                    {
                        _jogDailCenter = ImGui.GetIO().MousePos;
                        _jogDialValue = remapValue;
                    }

                    if (_jogDialValue == remapValue)
                    {
                        if (ImGui.IsItemActive())
                        {
                            modified = JogDialOverlay.Draw(ref value, ImGui.IsItemActivated(), _jogDailCenter, Double.NegativeInfinity, Double.PositiveInfinity,
                                                           0.01f);
                            if (modified)
                            {
                                remapValue.TypedInputValue.Value = (float)value;
                                remapValue.Input.IsDefault = false;
                                remapValue.DirtyFlag.Invalidate();
                            }
                        }
                        else
                        {
                            _jogDialValue = null;
                        }
                    }
                }
            }
            ImGui.PopID();
            return modified;
        }

        private static Vector2 _jogDailCenter;
        private static InputSlot<float> _jogDialValue;        
    }
}