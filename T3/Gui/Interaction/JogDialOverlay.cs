using System;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Gui.Styling;

namespace T3.Gui.Interaction
{
    /// <summary>
    /// Draws a circular dial to manipulate values with various speeds
    /// </summary>
    public static class JogDialOverlay
    {
        public static bool Draw(ref double value, bool restarted,  Vector2 center, double min = double.NegativeInfinity,
                                double max = double.PositiveInfinity,
                                float scale = 0.1f, bool clamp = false)
        {
            var modified = false;
            if (restarted)
            {
                _originalValue = value;
                _min = min;
                _max = max;
                _clamp = clamp;
                _center = center;
                _drawList = ImGui.GetForegroundDrawList();
                _io = ImGui.GetIO();
            }

            for (int ringIndex = 0; ringIndex < RingCount; ringIndex++)
            {
                modified |= DialRing.Draw(ringIndex);
            }

            if (modified)
                value = _value;
            
            return modified;
        }

        private const int RingCount = 2;
        //private static float[] _dampedValues = new float[RingCount];
        private static double _value;
        private static float _baseLog10Speed = 1;

        private static float AdjustedBaseLog10Scale
        {
            get
            {
                if (_io.KeyCtrl)
                {
                    return _baseLog10Speed++;
                }

                if (_io.KeyShift)
                {
                    return _baseLog10Speed--;
                }

                return _baseLog10Speed;
            }
        }

        private static double _originalValue;
        private static double _min;
        private static double _max;
        private static bool _clamp;

        private const float NeutralRadius = 15;

        private static ImDrawListPtr _drawList;
        private static ImGuiIOPtr _io;
        private static Vector2 _center;
        private static float DistanceToCenter => Vector2.Distance(_io.MousePos, _center);

        private static readonly Color SegmentColor = new Color(0.2f, 0.2f, 0.2f, 0.35f);
        private static readonly Color ActiveSegmentColor = new Color(1f, 1f, 1f, 0.5f);
        
        
        private static class DialRing
        {
            internal static bool Draw(int ringIndex)
            {
                _ringIndex = ringIndex;
                _drawList.AddCircle(_center,
                                    radius: Radius + RingWidth / 2,
                                    SegmentColor,
                                    num_segments: 64,
                                    thickness: RingWidth - Padding);

                DrawTicks();
                DrawValueLabels();

                var valueTickColor = new Color(1, 1, 1, IsActive ? 1 : 0.4f);
                DrawTick(_value / Pow, valueTickColor, lineThickness: IsActive ? 2 : 1);
                DrawValueDifference();
                return HandleInteraction();
            }

            private static void DrawTicks()
            {
                for (int lineIndex = 0; lineIndex < 100; lineIndex++)
                {
                    var f = lineIndex * 0.01f;
                    var alpha = (lineIndex % 10) == 0
                                    ? 0.3f
                                    : 0.1f;
                    alpha *= IsActive ? 1 : 0.4f;
                    var color = new Color(0, 0, 0, alpha);
                    var lineThickness = lineIndex == 0 ? 4 : 1;
                    var dialValue = ComputeDialValue(_value, f);
                    if (!_clamp || (dialValue >= _min && dialValue <= _max))
                    {
                        DrawTick(f, color, lineThickness);
                    }
                }
            }

            private static bool HandleInteraction()
            {
                if (!IsActive)
                    return false;
                
                var pLast = _io.MousePos - _io.MouseDelta - _center;
                var pNow = _io.MousePos - _center;
                
                var modified = false;
                var lastDialRatio = MathUtils.Fmod(-Math.Atan2(pLast.X, pLast.Y) / (Math.PI * 2), 1);
                lastDialRatio = Math.Round(lastDialRatio * 100f) / 100f;

                var dialRatio = MathUtils.Fmod(-Math.Atan2(pNow.X, pNow.Y) / (Math.PI * 2), 1);
                dialRatio = Math.Round(dialRatio * 100f) / 100f;

                var delta = dialRatio - lastDialRatio;
                if (delta > 0.5)
                    delta -= 1;
                else if (delta < -0.5)
                    delta += 1;

                if (!(Math.Abs(delta) > 0.001f))
                    return false;
                
                var offset = 0f;
                if (delta < 0)
                {
                    if (lastDialRatio >= 1 || lastDialRatio < dialRatio)
                    {
                        offset = -1;
                    }
                }
                else
                {
                    if (lastDialRatio >= 1 || lastDialRatio > dialRatio)
                    {
                        offset = 1;
                    }
                }

                var dialedValue = ComputeDialValue(_value, dialRatio);
                _value = dialedValue + offset * Pow;
                if (_clamp)
                {
                    _value = _value.Clamp(_min, _max);
                }
                return true;
            }
            
            private static void DrawTick(double f, Color color, int lineThickness)
            {
                var rads = -3.141578 * 2 * (f);
                var d = new Vector2((float)Math.Sin(rads), (float)Math.Cos(rads));
                _drawList.AddLine(_center + (Radius) * d, _center + (Radius + RingWidth) * d, color, lineThickness);
            }

            private static void DrawValueLabels()
            {
                var d = Vector2.UnitY;
                var lowerPart = MathUtils.Fmod(_value, Pow);
                var roundedUpperPart = _value - lowerPart;
                ImGui.PushFont(Fonts.FontLarge);
                _drawList.AddText(_center + (Radius + RingWidth / 2) * d + new Vector2(10f, 0),
                                  Color.Black,
                                  "" + (roundedUpperPart + Pow));

                var smallerLabel = $"{roundedUpperPart}";
                var size = ImGui.CalcTextSize(smallerLabel);
                _drawList.AddText(_center + (Radius + RingWidth / 2) * d + new Vector2(-10f - size.X, 0),
                                  Color.Black,
                                  smallerLabel);

                ImGui.PopFont();
            }

            private static void DrawValueDifference()
            {
                var rads1 = -3.141578 * 2 * (_originalValue + 0.5f - 0.25f);
                var rads2 = -3.141578 * 2 * (_value + 0.5f - 0.25f);

                var clamped = (rads1 - rads2).Clamp(-3.1415f * 2, 3.1415f * 2);
                rads2 = rads1 - clamped;
                _drawList.PathArcTo(
                                    _center,
                                    radius: Radius + RingWidth * 0.01f,
                                    a_min: -(float)rads1,
                                    a_max: -(float)rads2,
                                    num_segments: 64);
                _drawList.PathStroke(ActiveSegmentColor, false, 1);
            }

            private static double ComputeDialValue(double value, double dialRatio)
            {
                var pow = Math.Pow(10, Log10Scale - 1);
                var lowerPart = MathUtils.Fmod(value, pow);
                var roundedUpperPart = value - lowerPart;
                var newLowerPart = pow * dialRatio;
                var dialedValue = roundedUpperPart + newLowerPart;
                return dialedValue;
            }

            private static bool IsOuterSegment => _ringIndex == RingCount - 1;
            private static float Pow => (float)Math.Pow(10, Log10Scale - 1);
            private static int _ringIndex;
            private static float Log10Scale => AdjustedBaseLog10Scale + _ringIndex;
            private static float Radius => NeutralRadius + _ringIndex * RingWidth;

            private static bool  IsActive =>
                (DistanceToCenter > Radius && DistanceToCenter < Radius + RingWidth) ||
                (IsOuterSegment && DistanceToCenter > Radius + RingWidth);

            private const float RingWidth = 100;
            private const float Padding = 3;
        }

        // private static float AnimateTowards(ref float value, float target, float speed = 1)
        // {
        //     var direction = target - value;
        //     if (!(Math.Abs(direction) > 0.00001))
        //         return value;
        //     
        //     var offset = speed * ImGui.GetIO().DeltaTime;
        //     value = direction > 0
        //                 ? Math.Min(target, value + offset)
        //                 : Math.Max(target, value - offset);
        //     return value;
        // }
    }
}