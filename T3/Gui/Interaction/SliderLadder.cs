using System;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.Logging;
using T3.Gui.Styling;

namespace T3.Gui.Interaction
{
    /// <summary>
    /// Draws a range of virtual slider overlays
    /// </summary>
    internal static class SliderLadder
    {
        private class RangeDef
        {
            public readonly float YMin;
            public readonly float YMax;
            public readonly float ScaleFactor;
            public readonly string Label;

            public RangeDef(float yMin, float yMax, float scaleFactor, string label)
            {
                YMin = yMin;
                YMax = yMax;
                ScaleFactor = scaleFactor;
                Label = label;
            }
        }

        private static readonly RangeDef[] Ranges =
            {
                new(-4f * OuterRangeHeight, -3f * OuterRangeHeight, 1000, "1000"),
                new(-3f * OuterRangeHeight, -2f * OuterRangeHeight, 100, "100"),
                new(-2f * OuterRangeHeight, -1f * OuterRangeHeight, 10, "10"),

                new(-1f * OuterRangeHeight, 1f * OuterRangeHeight, 1, ""),

                new(1f * OuterRangeHeight, 2f * OuterRangeHeight, 0.1f, "0.1"),
                new(2f * OuterRangeHeight, 3f * OuterRangeHeight, 0.01f, "0.01"),
                new(3f * OuterRangeHeight, 4f * OuterRangeHeight, 0.001f, "0.001"),
            };

        private static RangeDef _lockedRange;
        private const float FadeInLaneOffset = 0.1f;
        private const float FadeInDuration = 0.1f;

        private const float LockDistance = 100;
        //private const float FadeInDelay = 0.1f;

        public static void Draw(ref double editValue, ImGuiIOPtr io, double min, double max, float scale, float timeSinceVisible, bool clamp, Vector2 center)
        {
            var foreground = ImGui.GetForegroundDrawList();
            //ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            const double initialDelay = 0.2;
            if (timeSinceVisible < initialDelay)
                _lockedRange = null;

            Vector2 pLast = io.MousePos - io.MouseDelta - center;
            Vector2 pNow = io.MousePos - center;
            if (!ImGui.IsMousePosValid())
            {
                _lastStepPosX = pNow.X = float.NaN;
            }

            if (timeSinceVisible < 0.032f || float.IsNaN(_lastStepPosX) )
            {
                _lastStepPosX = pNow.X;
            }

            var activeScaleFactor = 0.0;

            foreach (var range in Ranges)
            {
                var isVerticalMatch = pNow.Y > range.YMin && pNow.Y < range.YMax;
                var isWithinLockRange = Math.Abs(pNow.X) < LockDistance;

                var isActiveRange = isVerticalMatch && isWithinLockRange
                                    || _lockedRange == null && isVerticalMatch
                                    || _lockedRange == range;

                if (isActiveRange)
                {
                    activeScaleFactor = range.ScaleFactor;

                    _lockedRange = isWithinLockRange ? null : range;
                }

                var isCenterRange = Math.Abs(range.ScaleFactor - 1) < 0.001f;
                if (isCenterRange)
                    continue;

                // Draw
                var isLookedRange = _lockedRange == range;

                //var textColor = isActiveRange ? 
                var centerColor = !isActiveRange
                                      ? RangeFillColor
                                      : isLookedRange
                                          ? LockedRangeFillColor
                                          : ActiveRangeFillColor;

                var bMin = new Vector2(-RangeWidth, range.YMin) + center;
                var bMax = new Vector2(RangeWidth, range.YMax) + center;
                foreground.AddRectFilled(bMin, bMax, centerColor);
                foreground.AddRect(bMin, bMax, Color.Black);

                var labelSize = ImGui.CalcTextSize(range.Label);
                var pText = (bMin + bMax) / 2 - labelSize / 2;
                
                foreground.AddText(pText,
                                   isActiveRange ? Color.Black : Color.White,
                                   range.Label);
            }

            float deltaSinceLastStep = 0;
            if (!float.IsNaN(_lastStepPosX))
            {
                deltaSinceLastStep = pLast.X - _lastStepPosX;
            }

            var delta = deltaSinceLastStep / StepSize;
            if (io.KeyAlt)
            {
                ImGui.PushFont(Fonts.FontSmall);
                foreground.AddText(ImGui.GetMousePos() + new Vector2(10, 10), Color.Gray, "x0.01");
                ImGui.PopFont();

                delta *= 0.01f;
            }
            else if (io.KeyShift)
            {
                ImGui.PushFont(Fonts.FontSmall);
                foreground.AddText(ImGui.GetMousePos() + new Vector2(10, 10), Color.Gray, "x10");
                ImGui.PopFont();

                delta *= 10f;
            }

            if (!(Math.Abs(deltaSinceLastStep) >= StepSize))
                return;

            editValue += delta * activeScaleFactor * scale;
            if (activeScaleFactor > 1)
            {
                editValue = Math.Round(editValue / (activeScaleFactor * scale)) * (activeScaleFactor * scale);
            }

            if (clamp)
                editValue = editValue.Clamp(min, max);

            _lastStepPosX = pNow.X;
        }

        private const float StepSize = 3;
        private static float _lastStepPosX;
        private const float RangeWidth = 40;
        private const float OuterRangeHeight = 50;
        private static readonly Color RangeFillColor = new(0.3f, 0.3f, 0.3f);
        private static readonly Color ActiveRangeFillColor = new(0.8f, 0.8f, 0.8f);
        private static readonly Color LockedRangeFillColor = new(1f, 0.6f, 0.6f);
    }
}