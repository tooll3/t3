using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Utils;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.Interaction
{
    /// <summary>
    /// Draws a circular dial to manipulate values with various speeds
    /// </summary>
    public static class FreeDialOverlay
    {
        public static bool Draw(ref double value, bool restarted, Vector2 center, double min = double.NegativeInfinity,
                                double max = double.PositiveInfinity,
                                float scale = 0.1f, bool clamp = false)
        {
            var modified = false;
            _drawList = ImGui.GetForegroundDrawList();
            _io = ImGui.GetIO();
            
            if (restarted)
            {
                _baseLog10Speed = (int)(Math.Log10(scale)+3.5f);
                _value = value;
                _mousePositions.Clear();
                _center = _io.MousePos;
                _dampedRadius = 50;
            }

            _mousePositions.Add(_io.MousePos);

            if (_mousePositions.Count > 100)
            {
                _mousePositions.RemoveAt(0);
            }

            if (_mousePositions.Count > 1)
            {
                // Terminology
                // range - normalized angle from -0.5 ... 0.5 with 0 at current value
                // valueRange - delta value for complete revolution of current dial
                // tickInterval = Log10 delta vale between ticks.
                
                var p1 = _mousePositions[^1];
                var p2 = _mousePositions[^2];
                var radius = Vector2.Distance(_center, p1);
                
                _dampedRadius = MathUtils.Lerp(_dampedRadius, radius.Clamp(40,1000), 0.03f);
                _drawList.AddCircle(_center, _dampedRadius+25,  UiColors.BackgroundFull.Fade(0.1f), 128, 50);
                
                var valueRange = 2.5 * Math.Pow((_dampedRadius/500).Clamp(0.1f,2), 3) * 100 * scale * GetKeyboardScaleFactor();
                var tickInterval =  Math.Pow(10, (int)Math.Log10(valueRange * 3000 / _dampedRadius) - 2);
                
                var dir = p1 - _center;
                var valueAngle = MathF.Atan2(dir.X, dir.Y);
                
                var dirLast = p2 - _center;
                var valueAngleLast = MathF.Atan2(dirLast.X, dirLast.Y);

                var deltaAngle = DeltaAngle(valueAngle, valueAngleLast);
                
                _value += deltaAngle / (Math.PI * 2) * valueRange;
                value = Math.Round(_value / (tickInterval/10)) * tickInterval/10;
                
                var numberOfTicks = valueRange / tickInterval;
                
                var anglePerTick = 2*Math.PI / numberOfTicks;
                
                var valueTickOffsetFactor =  MathUtils.Fmod(_value, tickInterval) / tickInterval;
                var tickRatioAlignmentAngle = anglePerTick * valueTickOffsetFactor ;
                
                
                for (int tickIndex = -(int)numberOfTicks/2; tickIndex < numberOfTicks/2; tickIndex++)
                {
                    var f = MathF.Abs(tickIndex / ((float)numberOfTicks/2));
                    var negF = 1 - f;
                    var tickAngle = tickIndex * anglePerTick - valueAngle - tickRatioAlignmentAngle ;
                    var offset1 = new Vector2(MathF.Sin(-(float)tickAngle), MathF.Cos(-(float)tickAngle));
                    var valueAtTick = _value + (tickIndex * anglePerTick - tickRatioAlignmentAngle) / (2 * Math.PI) * valueRange;
                    var isPrimary =   Math.Abs(MathUtils.Fmod(valueAtTick + tickInterval * 5, tickInterval * 10) - tickInterval * 5) < tickInterval / 10;
                    
                    _drawList.AddLine(offset1 * _dampedRadius + _center,
                    offset1 * (_dampedRadius + (isPrimary ? 10 : 5f)) + _center,
                        UiColors.ForegroundFull.Fade(negF * (isPrimary ? 1 : 0.5f)),
                        1
                    );
                                        
                    if (isPrimary)
                    {
                        _drawList.AddText(Fonts.FontSmall, 
                                          Fonts.FontSmall.FontSize, 
                                          offset1 * (_dampedRadius + 30) + _center + new Vector2(-10,-Fonts.FontSmall.FontSize/2), 
                                          UiColors.ForegroundFull.Fade(negF), 
                                          $"{valueAtTick:0.0}");
                    }
                }
                _drawList.AddText(_io.MousePos + new Vector2(100,100), Color.White, 
                                  $"da:{deltaAngle:0.00}\n" 
                                  + $"dTick:{tickInterval:0.00}\n"
                                  + $"valueAngle: {valueAngle: 0.00}");
            }

            return true;
        }

        private static float _dampedRadius = 0;
        private static Vector2 _center = Vector2.Zero;

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
        
        /** The precise value before rounding. This used for all internal calculations. */
        private static double _value; 

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
        
        private static readonly List<Vector2> _mousePositions = new(100);
        
        private static float _baseLog10Speed = 1;

        private static double GetKeyboardScaleFactor()
        {
            if (_io.KeyAlt)
            {
                return 10;
            }

            if (_io.KeyShift)
            {
                return 0.1;
            }

            return 1;
        }
        

        private static ImDrawListPtr _drawList;
        private static ImGuiIOPtr _io;
    }
}