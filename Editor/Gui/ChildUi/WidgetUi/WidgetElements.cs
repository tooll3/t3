using System;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.ChildUi.WidgetUi
{
    /// <summary>
    /// A set of helper methods for implementing consistent ChildUi 
    /// </summary>
    public static class WidgetElements
    {
        public static void DrawTitle(ImDrawListPtr drawList, ImRect widgetRect, string title)
        {
            var canvasScale = GraphCanvas.Current.Scale.Y / T3Ui.UiScaleFactor;
            var font = canvasScale > ScaleFactors.LargerScale
                           ? Fonts.FontNormal
                           : Fonts.FontSmall;

            ImGui.PushFont(font);

            var fadingColor = T3Style.Colors.TextWidgetTitle
                                     .Fade(MathUtils.NormalizeAndClamp
                                               (
                                                canvasScale,
                                                ScaleFactors.NormalScale,
                                                ScaleFactors.BigScale));
            drawList.AddText(widgetRect.Min + new Vector2(5, 2),
                             fadingColor,
                             title);

            ImGui.PopFont();
        }

        public static void DrawPrimaryValue(ImDrawListPtr drawList, ImRect widgetRect, string formattedValue)
        {
            var canvasScale = GraphCanvas.Current.Scale.Y;
            var font = canvasScale < ScaleFactors.NormalScale
                           ? Fonts.FontSmall
                           : canvasScale > ScaleFactors.BigScale
                               ? Fonts.FontLarge
                               : Fonts.FontNormal;

            ImGui.PushFont(font);
            var fadingColor = T3Style.Colors.TextWidgetTitle
                                     .Fade(MathUtils.NormalizeAndClamp
                                               (
                                                canvasScale,
                                                ScaleFactors.SmallerScale,
                                                ScaleFactors.SmallScale));

            var labelSize = ImGui.CalcTextSize(formattedValue);
            drawList.AddText(new Vector2
                                 (widgetRect.Min.X + 5,
                                  widgetRect.Max.Y - labelSize.Y -2),
                             fadingColor, formattedValue);

            ImGui.PopFont();
        }
        
        public static bool DrawRateLabelWithTitle(InputSlot<float> inputSlot, ImRect selectableScreenRect, ImDrawListPtr drawList,  string nodeLabel)
        {
            var rate = inputSlot.Input.Value is InputValue<float> floatValue ? floatValue.Value : inputSlot.Value;
            var modified = false;
            
            var currentRateIndex = SpeedRate.FindClosestRateIndex(rate);            
            var formattedValue = currentRateIndex == -1 
                            ? $"{rate:0.0}" 
                            : SpeedRate.RelevantRates[currentRateIndex].Label;
            DrawPrimaryValue(drawList, selectableScreenRect, formattedValue);
            
            var isActive = false;
            var editUnlocked = ImGui.GetIO().KeyCtrl;
            
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
            
            DrawTitle(drawList, selectableScreenRect, nodeLabel);
            
            return modified;
        }        

        private static class WidgetHeights
        {
            public const float Tiny = 5;
            public const float Smaller = 10;
            public const float Small = 15;
            public const float Normal = 25;
            public const float Larger = 40;
            public const float Big = 65;
        }

        private static class ScaleFactors
        {
            public const float TinyScale = WidgetHeights.Tiny / WidgetHeights.Normal;
            public const float SmallerScale = WidgetHeights.Smaller / WidgetHeights.Normal;
            public const float SmallScale = WidgetHeights.Small / WidgetHeights.Normal;
            public const float NormalScale = 1;
            public const float LargerScale = WidgetHeights.Larger / WidgetHeights.Normal;
            public const float BigScale = WidgetHeights.Big / WidgetHeights.Normal;
        }
    }
}