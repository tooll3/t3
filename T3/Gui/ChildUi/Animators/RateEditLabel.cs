using System;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Styling;
using UiHelpers;

namespace T3.Gui.ChildUi.Animators
{
    public static class RateEditLabel
    {
        public static bool Draw(InputSlot<float> inputSlot, ImRect selectableScreenRect, ImDrawListPtr drawList,  string nodeLabel)
        {
            var rate = inputSlot.Value;
            var modified = false;
            var h = selectableScreenRect.GetHeight();
            

            // Draw Rate Label
            var font = h > 33
                           ? Fonts.FontLarge
                           : (h > 17
                                  ? Fonts.FontNormal
                                  : Fonts.FontSmall);

            ImGui.PushFont(font);

            var currentRateIndex = SpeedRate.FindCurrentRateIndex(rate);

            ImGui.SetCursorScreenPos(selectableScreenRect.Min + Vector2.One * 4);

            var label = currentRateIndex == -1 
                            ? $"{rate:0.0}" 
                            : SpeedRate.RelevantRates[currentRateIndex].Label;
            var labelSize = ImGui.CalcTextSize(label);
            drawList.AddText(new Vector2(selectableScreenRect.Min.X + 3, selectableScreenRect.Max.Y - labelSize.Y), Color.White, label);
            ImGui.PopFont();

            var isActive = false;
            var editUnlocked = ImGui.GetIO().KeyCtrl;
            //var highlight = editUnlocked;
            
            // Speed Interaction
            var speedRect = selectableScreenRect;
            speedRect.Max.X = speedRect.Min.X +  speedRect.GetWidth() * 0.2f;
            ImGui.SetCursorScreenPos(speedRect.Min);

            
            if (editUnlocked)
            {
                ImGui.InvisibleButton("rateButton", speedRect.GetSize());
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
                }

                isActive = ImGui.IsItemActive();
            }
            
            if (isActive && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                var dragDelta = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left, 1);
                if (Math.Abs(dragDelta.Y) > 40)
                {
                    if (dragDelta.Y > 0 && currentRateIndex > 0)
                    {
                        inputSlot.TypedInputValue.Value = SpeedRate.RelevantRates[currentRateIndex - 1].Factor;
                        modified = true;
                    }
                    else if (dragDelta.Y < 0 && currentRateIndex < SpeedRate.RelevantRates.Length - 1)
                    {
                        inputSlot.TypedInputValue.Value = SpeedRate.RelevantRates[currentRateIndex + 1].Factor;
                        modified = true;
                    }

                    ImGui.ResetMouseDragDelta();
                }
            }

            var highlight = editUnlocked && (isActive  || !ImGui.IsAnyItemActive());
            
            // Draw Type label
            if (h > 30)
            {
                ImGui.PushFont(Fonts.FontSmall);
                drawList.AddText(selectableScreenRect.Min + new Vector2(4, 2), highlight ? T3Style.Colors.ValueLabelColorHover : T3Style.Colors.ValueLabelColor, nodeLabel);
                ImGui.PopFont();
            }
            
            return modified;
        }
    }
}