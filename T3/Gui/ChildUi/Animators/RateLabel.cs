using System;
using System.Numerics;
using ImGuiNET;
using T3.Gui.Styling;
using UiHelpers;

namespace T3.Gui.ChildUi.Animators
{
    public static class RateLabel
    {
        public static bool Draw(ref float rate, ImRect selectableScreenRect, ImDrawListPtr drawList)
        {
            var modified = false;
            var h = selectableScreenRect.GetHeight();
            // Draw Type label
            if (h > 30)
            {
                ImGui.PushFont(Fonts.FontSmall);
                drawList.AddText(selectableScreenRect.Min + new Vector2(4, 2), Color.White, "Jitter2D");
                ImGui.PopFont();
            }

            // Draw Rate Label
            var font = h > 33
                           ? Fonts.FontLarge
                           : (h > 17
                                  ? Fonts.FontNormal
                                  : Fonts.FontSmall);

            ImGui.PushFont(font);

            var currentRateIndex = SpeedRate.FindCurrentRateIndex(rate);

            ImGui.SetCursorScreenPos(selectableScreenRect.Min + Vector2.One * 4);

            var label = currentRateIndex == -1 ? "?" : SpeedRate.RelevantRates[currentRateIndex].Label;
            var labelSize = ImGui.CalcTextSize(label);
            drawList.AddText(new Vector2(selectableScreenRect.Min.X + 3, selectableScreenRect.Max.Y - labelSize.Y), Color.White, label);
            ImGui.PopFont();

            // Speed Interaction
            var speedRect = selectableScreenRect;
            speedRect.Max.X = speedRect.Min.X + speedRect.GetHeight();
            ImGui.SetCursorScreenPos(speedRect.Min);
            ImGui.InvisibleButton("rateButton", speedRect.GetSize());
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
            }

            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0))
            {
                var dragDelta = ImGui.GetMouseDragDelta(0, 1);
                if (Math.Abs(dragDelta.Y) > 40)
                {
                    if (dragDelta.Y > 0 && currentRateIndex > 0)
                    {
                        rate = SpeedRate.RelevantRates[currentRateIndex - 1].Factor;
                        modified = true;
                    }
                    else if (dragDelta.Y < 0 && currentRateIndex < SpeedRate.RelevantRates.Length - 1)
                    {
                        rate = SpeedRate.RelevantRates[currentRateIndex + 1].Factor;
                        modified = true;
                    }

                    ImGui.ResetMouseDragDelta();
                }
            }

            return modified;
        }
    }
}