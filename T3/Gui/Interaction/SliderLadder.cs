using System;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Gui.Styling;

namespace T3.Gui.Interaction
{
    /// <summary>
    /// Draws a range of virtual slider overlays
    /// </summary>
    static class SliderLadder
    {
        private class RangeDef
        {
            public readonly float YMin;
            public readonly float YMax;
            public readonly float ScaleFactor;
            public readonly string Label;
            public readonly float FadeInDelay;

            public RangeDef(float yMin, float yMax, float scaleFactor, string label, float fadeInDelay)
            {
                YMin = yMin;
                YMax = yMax;
                ScaleFactor = scaleFactor;
                Label = label;
                FadeInDelay = fadeInDelay;
            }
        }

        private static readonly RangeDef[] Ranges =
            {
                new RangeDef(-4f * OuterRangeHeight, -3f * OuterRangeHeight, 1000, "x1000", 2),
                new RangeDef(-3f * OuterRangeHeight, -2f * OuterRangeHeight, 100, "x100", 1),
                new RangeDef(-2f * OuterRangeHeight, -1f * OuterRangeHeight, 10, "x10", 0),
                new RangeDef(-1f * OuterRangeHeight, 1f * OuterRangeHeight, 1, "", 0),
                new RangeDef(1f * OuterRangeHeight, 2f * OuterRangeHeight, 0.1f, "x0.1", 0),
                new RangeDef(2f * OuterRangeHeight, 3f * OuterRangeHeight, 0.01f, "x0.01", 1),
            };

        private static RangeDef _lockedRange;

        public static void Draw(ref double _editValue, ImGuiIOPtr io, double min, double max, float scale, float timeSinceVisible, bool clamp, Vector2 _center)
        {
            var foreground = ImGui.GetForegroundDrawList();
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (timeSinceVisible < 0.2)
                _lockedRange = null;

            var pLast = io.MousePos - io.MouseDelta - _center;
            var pNow = io.MousePos - _center;
            if (timeSinceVisible < 0.032f)
            {
                _lastStepPosX = pNow.X;
            }

            float activeScaleFactor = 0;

            foreach (var range in Ranges)
            {
                var isActiveRange = range == _lockedRange
                                    || (_lockedRange == null && pNow.Y > range.YMin && pNow.Y < range.YMax);
                var opacity = (timeSinceVisible * 4 - range.FadeInDelay / 4).Clamp(0, 1);

                var isCenterRange = Math.Abs(range.ScaleFactor - 1) < 0.001f;
                if (isActiveRange)
                {
                    activeScaleFactor = range.ScaleFactor;
                    if (_lockedRange == null && Math.Abs(ImGui.GetMouseDragDelta().X) > 30)
                    {
                        _lockedRange = range;
                    }
                }

                if (!isCenterRange)
                {
                    var centerColor = (isActiveRange ? RangeActiveColor : RangeCenterColor) * opacity;

                    foreground.AddRectFilledMultiColor(
                                                       new Vector2(-RangeWidth, range.YMin) + _center,
                                                       new Vector2(0, range.YMax - RangePadding) + _center,
                                                       RangeOuterColor,
                                                       centerColor,
                                                       centerColor,
                                                       RangeOuterColor
                                                      );

                    foreground.AddRectFilledMultiColor(
                                                       new Vector2(0, range.YMin) + _center,
                                                       new Vector2(RangeWidth, range.YMax - RangePadding) + _center,
                                                       centerColor,
                                                       RangeOuterColor,
                                                       RangeOuterColor,
                                                       centerColor
                                                      );
                    foreground.AddText(Fonts.FontLarge,
                                       Fonts.FontLarge.FontSize,
                                       new Vector2(-20, range.YMin) + _center,
                                       isActiveRange ? (Color.White * opacity) : Color.Black,
                                       range.Label);
                }
            }

            var deltaSinceLastStep = pLast.X - _lastStepPosX;
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

            _editValue += delta * activeScaleFactor * scale;
            if (activeScaleFactor > 1)
            {
                _editValue = Math.Round(_editValue / (activeScaleFactor * scale)) * (activeScaleFactor * scale);
            }

            if (clamp)
                _editValue = _editValue.Clamp(min, max);

            _lastStepPosX = pNow.X;
        }

        private const float StepSize = 3;
        private static float _lastStepPosX;
        private const float RangeWidth = 300;
        private const float OuterRangeHeight = 50;
        private const float RangePadding = 1;
        private static readonly Color RangeCenterColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        private static readonly Color RangeOuterColor = new Color(0.3f, 0.3f, 0.3f, 0.0f);
        private static readonly Color RangeActiveColor = new Color(0.0f, 0.0f, 0.0f, 0.7f);
    }
}