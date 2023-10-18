using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using T3.Core.Utils;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Interaction
{
    /// <remarks>
    /// Terminology
    /// valueRange - delta value for complete revolution of current dial
    /// tickInterval = Log10 delta vale between ticks.
    /// </remarks>
    public static class LinearFlexEditOverlay
    {
        public static void Draw(ref double roundedValue, bool restarted, Vector2 center, double min = double.NegativeInfinity,
                                double max = double.PositiveInfinity,
                                float scale = 0.1f, bool clamp = false)
        {
            var drawList = ImGui.GetForegroundDrawList();
            _io = ImGui.GetIO();

            if (restarted)
            {
                _value = roundedValue;
                _center = _io.MousePos;
                _dampedDistance = 50;
                _dampedAngleVelocity = 0;
                _dampedModifierScaleFactor = 1;
                _lastXOffset = 0;
                _framesSinceLastMove = 120;
                _originalValue = roundedValue;
                _framesSinceStart = 0;
            }



            //_mousePositions.Add(_io.MousePos);
            // if (_mousePositions.Count > 100)
            // {
            //     _mousePositions.RemoveAt(0);
            // }
            _framesSinceStart++;
            //
            // if (_framesSinceStart <= 1)
            //     return;

            var p1 = _io.MousePos;
            var mouseYDistance = _center.Y - _io.MousePos.Y; // Vector2.Distance(_center, p1);

            // Update angle...
            var mousePosX = _io.MousePos.X;
            var xOffset = mousePosX - _center.X;
            var deltaX = xOffset - _lastXOffset;
            _lastXOffset = xOffset;

            var hasMoved = Math.Abs(deltaX) > 0.015f;
            if (hasMoved)
            {
                _framesSinceLastMove = 0;
            }
            else
            {
                _framesSinceLastMove++;
            }

            _dampedAngleVelocity = MathUtils.Lerp(_dampedAngleVelocity, (float)deltaX, 0.06f);

            // Update radius and value range
            _dampedDistance = mouseYDistance;
            var log10yDistance = 100;
            var normalizedLogDistanceForLog10 = _dampedDistance / log10yDistance;

            // Value range and tick interval 
            _dampedModifierScaleFactor = MathUtils.Lerp(_dampedModifierScaleFactor, GetKeyboardScaleFactor(), 0.1f);

            var valueRange = (Math.Pow(10, normalizedLogDistanceForLog10)) * scale * _dampedModifierScaleFactor * 600;

            var log10 = Math.Log10(valueRange);
            var iLog10 = (int)log10;
            var logRemainder = log10 - iLog10;
            var tickValueInterval = Math.Pow(10, iLog10 - 1);

            float Width = 750;

            // Update value...
            _value += deltaX / Width * valueRange;
            roundedValue = _io.KeyCtrl ? _value : Math.Round(_value / (tickValueInterval/10)) * (tickValueInterval/10);

            var rSize = new Vector2(Width, 40);
            var rCenter = new Vector2(mousePosX, _io.MousePos.Y - rSize.Y);
            var rect = new ImRect(rCenter - rSize / 2, rCenter + rSize / 2);
            drawList.AddRectFilled(rCenter - rSize / 2, rCenter + rSize / 2, UiColors.BackgroundFull.Fade(0.6f), 8);
            
            var numberOfTicks = valueRange / tickValueInterval;
            
            var valueTickRemainder = MathUtils.Fmod(_value, tickValueInterval) ;
            
            // Draw ticks with labels
            for (var tickIndex = -(int)numberOfTicks/2; tickIndex < numberOfTicks/2; tickIndex++)
            {
                var f = MathF.Pow(MathF.Abs(tickIndex / ((float)numberOfTicks/2)), 2f);
                var negF = 1 - f;
                var valueAtTick = tickIndex * tickValueInterval + _value - valueTickRemainder;
                GetXForValueIfVisible(valueAtTick, valueRange, mousePosX, Width,out var tickX);
                var isPrimary =   Math.Abs(MathUtils.Fmod(valueAtTick + tickValueInterval * 5, tickValueInterval * 10) - tickValueInterval * 5) < tickValueInterval / 10;
                var isPrimary2 =   Math.Abs(MathUtils.Fmod(valueAtTick + tickValueInterval * 50, tickValueInterval * 100) - tickValueInterval * 50) < tickValueInterval / 100;
                    
                var fff = MathUtils.SmootherStep((float)1,0.8f, (float)logRemainder);
                drawList.AddLine(
                                 new Vector2(tickX, rect.Max.Y),
                                 new Vector2(tickX, rect.Max.Y-10),
                                 UiColors.ForegroundFull.Fade(negF * (isPrimary ? 1 : 0.5f * fff)),
                                 1
                                );
            
                var font = isPrimary2 ? Fonts.FontBold : Fonts.FontSmall;
                var v = Math.Abs(valueAtTick) < 0.0001 ? 0 : valueAtTick;
                var label = $"{v:G5}";
                        
                ImGui.PushFont(font);
                var size = ImGui.CalcTextSize(label);
                ImGui.PopFont();
                        
                var ff = (1-(float)logRemainder*2);
                if (isPrimary2 || ff < 1)
                {
                    drawList.AddText(font, 
                                     font.FontSize, 
                                     new Vector2(tickX-1, rect.Max.Y - 30),
                                     UiColors.BackgroundFull.Fade(negF*ff), 
                                     label);
                    
                    drawList.AddText(font, 
                                     font.FontSize, 
                                     new Vector2(tickX+1, rect.Max.Y - 30),
                                     UiColors.BackgroundFull.Fade(negF*ff), 
                                     label);
                    
                    var fadeOut = (isPrimary ? 1 :ff)  * 0.7f;
                    drawList.AddText(font, 
                                     font.FontSize, 
                                     new Vector2(tickX, rect.Max.Y - 30),
                                     UiColors.ForegroundFull.Fade(negF * (isPrimary2 ? 1 : fadeOut)), 
                                     label);

                    if (isPrimary)
                    {
                        // for (var yIndex = 1; yIndex < 5; yIndex++)
                        // {
                        //     var centerPoint = new Vector2(tickX, rect.GetCenter().Y - log10yDistance * (yIndex - (float)logRemainder));
                        //     drawList.AddCircleFilled(centerPoint, 4, UiColors.ForegroundFull);
                        //     drawList.AddText(font, 
                        //                      font.FontSize, 
                        //                      centerPoint + new Vector2(10,0),
                        //                      UiColors.ForegroundFull, 
                        //                      $"{v * Math.Pow(10,yIndex):G5}");
                        //     
                        // }
                    }
                }
            }
                
            // Draw previous value
            // {
            //     var visible = IsValueVisible(_originalValue, valueRange);
            //     if (visible)
            //     {
            //         var originalValueAngle= ComputeAngleForValue(_originalValue, valueRange, mouseAngle);
            //         var direction = new Vector2(MathF.Sin(originalValueAngle), MathF.Cos(originalValueAngle));
            //         drawList.AddLine(direction * _dampedRadius + _center,
            //                          direction * (_dampedRadius - 10) + _center,
            //                          UiColors.StatusActivated.Fade(0.8f),
            //                          2
            //                         );
            //     }
            // }
                
            // Draw Value range
            {
                var rangeMin = _value - valueRange / 2;
                var rangeMax = _value + valueRange / 2;
                if (!(min < rangeMin && max < rangeMin || (min > rangeMax && max > rangeMax)))
                {
                    const float innerOffset = 4;
                    var visibleMinValue= Math.Max(min, rangeMin);
                    var visibleMaxValue= Math.Min(max, rangeMax);

                    var y = rect.Max.Y - 1.5f;
                    GetXForValueIfVisible(visibleMinValue, valueRange, mousePosX, Width, out var visibleMinX);
                    GetXForValueIfVisible(visibleMaxValue, valueRange, mousePosX, Width, out var visibleMaxX);
                    drawList.AddLine(new Vector2(visibleMinX,y), 
                                     new Vector2(visibleMaxX,y),
                                     UiColors.ForegroundFull.Fade(0.5f),
                                     2);
                }
            }

            // Current value at mouse
            {
                if (!GetXForValueIfVisible(roundedValue, valueRange, mousePosX, Width, out var screenX))
                    return;
                
                var label = $"{roundedValue:0.00}\n";
                var labelSize = ImGui.CalcTextSize(label);
                drawList.AddRectFilled(
                                       new Vector2(screenX - labelSize.X/2- 10, rect.Max.Y),
                                       new Vector2(screenX + labelSize.X/2+ 10, rect.Max.Y+25),
                                       UiColors.BackgroundFull.Fade(0.5f),
                                       5
                                      );
                drawList.AddLine(new Vector2(screenX, rect.Min.Y),
                                 new Vector2(screenX, rect.Max.Y+5),
                                 UiColors.StatusActivated.Fade(0.7f),
                                 2
                                );
                drawList.AddText(Fonts.FontBold,
                                 Fonts.FontBold.FontSize,
                                 new Vector2(screenX - labelSize.X/2, rect.Max.Y+3), 
                                 Color.White.Fade(1), 
                                 label 
                                );
            }
        }

        private static bool IsValueVisible(double value, double valueRange)
        {
            return Math.Abs(_value - value) <= valueRange / 2 + 0.001;
        }

        private static float GetXForValue(double value, double valueRange, float mouseAngle)
        {
            return (float)( (_value - value)  / valueRange + mouseAngle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool GetXForValueIfVisible(double v, double valueRange, float mouseX, float width, out float x)
        {
            x = float.NaN;
            if(!IsValueVisible(v, valueRange))
                return false;

            x= (float)( (v - _value)  / valueRange * width + mouseX);
            return true;
        }


        private static double DeltaAngle(double angle1, double angle2)
        {
            angle1 = (angle1 + Math.PI) % (2 * Math.PI) - Math.PI;
            angle2 = (angle2 + Math.PI) % (2 * Math.PI) - Math.PI;

            var delta = angle2 - angle1;
            return delta switch
                       {
                           > Math.PI  => delta - 2 * Math.PI,
                           < -Math.PI => delta + 2 * Math.PI,
                           _          => delta
                       };
        }
        

        private static bool CalculateIntersection(Vector2 p1A, Vector2 p1B, Vector2 p2A, Vector2 p2B, out Vector2 intersection)
        {
            double a1 = p1B.Y - p1A.Y;
            double b1 = p1A.X - p1B.X;
            double c1 = a1 * p1A.X + b1 * p1A.Y;

            double a2 = p2B.Y - p2A.Y;
            double b2 = p2A.X - p2B.X;
            double c2 = a2 * p2A.X + b2 * p2A.Y;

            double determinant = a1 * b2 - a2 * b1;

            if (Math.Abs(determinant) < 1e-6) // Lines are parallel
            {
                intersection =Vector2.Zero;
                return false;
            }

            var x = (b2 * c1 - b1 * c2) / determinant;
            var y = (a1 * c2 - a2 * c1) / determinant;
            intersection = new Vector2((float)x, (float)y);
            return true;
        }
        

        private static double GetKeyboardScaleFactor()
        {
            if (_io.KeyAlt)
                return 10;

            if (_io.KeyShift)
                return 0.1;

            return 1;
        }
        
        /** The precise value before rounding. This used for all internal calculations. */
        private static double _value; 

        private static float _dampedDistance;
        private static Vector2 _center = Vector2.Zero;
        private static float _dampedAngleVelocity;
        private static double _lastXOffset;
        private static double _dampedModifierScaleFactor;
        private static float _dampedDialValueAngle;
        private static int _framesSinceLastMove;
        private static double _originalValue;
        //private static readonly List<Vector2> _mousePositions = new(100);
        //private static ImDrawListPtr _drawList;
        private static ImGuiIOPtr _io;
        private static int _framesSinceStart;
    }
}