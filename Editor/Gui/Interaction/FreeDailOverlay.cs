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
                _min = min;
                _max = max;
                _clamp = clamp;
                
                _mousePositions.Clear();
                _dampedRadius = 0;
            }

            var moveDelta = _mousePositions.Count == 0 ? 999 : (_io.MousePos- _mousePositions[^1]).Length();
            var hasMoved = moveDelta > 1.4f;
            if (hasMoved)
            {
                _mousePositions.Add(_io.MousePos);
            }

            const int smoothOffset = 2;

            if (_mousePositions.Count > 100)
            {
                _mousePositions.RemoveAt(0);
            }


            if (_mousePositions.Count > smoothOffset)
            {
                var list2 = new List<Vector2>(100);
                var smoothP = _mousePositions[0];
                
                foreach (var p in _mousePositions)
                {
                    smoothP = MathUtils.Lerp(smoothP, p, 0.2f);
                    list2.Add(smoothP);
                }
                
                Vector2 sumIntersections = Vector2.Zero;
                float count = 0;
                
                Vector2 nLast = Vector2.Zero;
                Vector2 pLast2 = Vector2.Zero;
                Vector2 lastIntersection = Vector2.Zero;
                
                for (var index = smoothOffset; index < list2.Count - smoothOffset ; index++)
                {
                    var pLast = list2[index-smoothOffset];
                    var p = list2[index];
                    var pNext = list2[index+smoothOffset];
                    
                    var d1 = pLast - p;
                    var d2 = pNext - p;

                    
                    var n1 = Vector2.Normalize( new Vector2(d1.X, d1.Y));
                    var n2 = Vector2.Normalize( new Vector2(d2.X, d2.Y));
                    var n = Vector2.Normalize(n1 + n2) * 1500;

                    var isCurvature = !float.IsNaN(n.X);
                    // _drawList.AddCircle(p, 3,  isCurvature ? Color.Green : Color.Red);
                    //_drawList.AddLine(p,p +n, Color.White.Fade(0.1f));

                    if (isCurvature)
                    {
                        if (CalculateIntersection(p, p+ n, pLast2, p+ nLast, out var intersection))
                        {
                            var dToLastIntersection = Vector2.Distance(intersection, lastIntersection);
                            var w1 = 1;// MathUtils.SmootherStep( 300,200, dToLastIntersection);
                            
                            
                            var dToPoint = Vector2.Distance(intersection, p);
                            var w2 = MathUtils.SmootherStep(10, 20, dToPoint) * MathUtils.SmootherStep(500,300, dToPoint);
                            
                            var weight = w1 * w2* (float)index / list2.Count;
                            sumIntersections += intersection * weight;
                            count+= weight;
                            if(weight * 10 > 1)
                                _drawList.AddCircle(intersection, weight * 10,  Color.Blue);
                            
                            _drawList.AddLine(lastIntersection, intersection, Color.White.Fade(0.1f));
                            lastIntersection = intersection;
                        }

                        nLast = n;
                        pLast2 = p;
                    }
                }

                
                if (count > 0)
                {
                    var averageCenter = sumIntersections/count;
                    var p1 = list2[^1];
                    var p2 = list2[^2];
                    var d = PointSideToLine(averageCenter, p1, p2 - p1);
                    
                    var radius = Vector2.Distance(averageCenter, pLast2);
                    
                    _dampedRadius = MathUtils.Lerp(_dampedRadius, radius, 0.1f);
                    var finalRadius = _dampedRadius.Clamp(30, 500);
                    _drawList.AddCircle(averageCenter, finalRadius,  d > 0 ? Color.Blue : Color.Red);

                    if (hasMoved && d != 0)
                    {
                        var speedDelta = Math.Pow(10,(finalRadius -30) / 500 - 2.5) * _baseLog10Speed;
                        value += speedDelta * moveDelta * d;

                        var log10 = (int)(Math.Log10(speedDelta) );
                        var roundFactor = Math.Pow(10, log10);
                        value = Math.Round(value / roundFactor) * roundFactor;
                        if (clamp)
                        {
                            value = value.Clamp(min, max);
                        }
                    }
                }
            }
            
            // for (int ringIndex = 0; ringIndex < RingCount; ringIndex++)
            // {
            //     modified |= DialRing.Draw(ringIndex);
            // }
            //
            // if (modified)
            // {
            //     value = ClampedValue;
            // }
            

            return true;
        }

        private static float _dampedRadius = 0;

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

        private static int PointSideToLine(Vector2 p, Vector2 linePoint, Vector2 direction)
        {
            var v1 = p - linePoint;

            double crossProduct = (v1.X * direction.Y) - (v1.Y * direction.X);
        
            if (Math.Abs(crossProduct) < 1e-6) // Point is on the line
            {
                return 0;
            }

            if (crossProduct < 0) // Point is on the left side
            {
                return -1;
            }

            // Point is on the right side
            return 1;
        }

        private static readonly List<Vector2> _mousePositions = new(100);
        
        private static float _baseLog10Speed = 1;

        private static float AdjustedBaseLog10Scale
        {
            get
            {
                if (_io.KeyAlt)
                {
                    return _baseLog10Speed+1;
                }

                if (_io.KeyShift)
                {
                    return _baseLog10Speed-1;
                }

                return _baseLog10Speed;
            }
        }

        private static double _min;
        private static double _max;
        private static bool _clamp;


        private static ImDrawListPtr _drawList;
        private static ImGuiIOPtr _io;
    }
}