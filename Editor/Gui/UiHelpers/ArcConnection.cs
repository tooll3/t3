using System;
using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Utils;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.UiHelpers
{
    static class ArcConnection
    {
        private static readonly Color OutlineColor = new(0.1f, 0.1f, 0.1f, 0.6f);

        /// <summary>
        /// Draws an arc connection line.
        /// </summary>
        /// <remarks>
        /// Assumes that B is the tight node connection direction is bottom to top
        /// 
        ///                                dx
        ///        +-------------------+
        ///        |      A            +----\  rB+---------------+
        ///        +-------------------+rA   \---+      B        |
        ///                                      +---------------+
        /// </remarks>
        private const float Pi = 3.141592f;

        private const float TAU = Pi / 180;

        public static bool Draw(ImRect rectA, Vector2 pointA, ImRect rectB, Vector2 pointB, Color color, float thickness, float currentCanvasScale, ref Vector2 hoverPosition)
        {
            //var currentCanvasScale = GraphCanvas.Current.Scale.X.Clamp(0.2f,2f);
            pointA.X -= (3-1 * currentCanvasScale).Clamp(1,1);
            pointB.X += (4-4 * currentCanvasScale).Clamp(0,4);
            
            var r2 = rectA;
            r2.Add(rectB);
            r2.Add(pointB);
            r2.Add(pointA);
            
            if (!ImGui.IsRectVisible(r2.Min, r2.Max))
                return false;
            
            var drawList = ImGui.GetWindowDrawList();

            var fallbackRectSize = new Vector2(120, 50) * currentCanvasScale;
            if (rectA.GetHeight() < 1)
            {
                var rectAMin = new Vector2(pointA.X - fallbackRectSize.X, pointA.Y - fallbackRectSize.Y / 2);
                rectA = new ImRect(rectAMin, rectAMin + fallbackRectSize);
            }

            if (rectB.GetHeight() < 1)
            {
                var rectBMin = new Vector2(pointB.X, pointB.Y - fallbackRectSize.Y / 2);
                rectB = new ImRect(rectBMin, rectBMin + fallbackRectSize);
            }

            var d = pointB - pointA;
            const float limitArcConnectionRadius = 50;
            var maxRadius = limitArcConnectionRadius * currentCanvasScale*2f;
            const float shrinkArkAngle = 0.8f;
            var edgeFactor = (0.35f * (currentCanvasScale- 0.3f)).Clamp(0.01f, 0.35f); // 0 -> overlap  ... 1 concentric around node edge
            const float outlineWidth = 3;
            var edgeOffset =  1 * currentCanvasScale;


            var pointAOrg = pointA;

            if (d.Y > -1 && d.Y < 1 && d.X > 2)
            {
                drawList.AddLine(pointA, pointB, color, thickness);
                return false;
            }

            //drawList.PathClear();
            var aAboveB = d.Y > 0;
            if (aAboveB)
            {
                var cB = rectB.Min;
                var rB = (pointB.Y - cB.Y) * edgeFactor + edgeOffset;

                var exceededMaxRadius = d.X - rB > maxRadius;
                if (exceededMaxRadius)
                {
                    pointA.X += d.X - maxRadius - rB;
                    d.X = maxRadius + rB;
                }

                var cA = rectA.Max;
                var rA = cA.Y - pointA.Y;
                cB.Y = pointB.Y - rB;

                if (d.X > rA + rB)
                {
                    var horizontalStretch = d.X / d.Y > 1;
                    if (horizontalStretch)
                    {
                        var f = d.Y / d.X;
                        var alpha = (float)(f * 90 + Math.Sin(f * 3.1415f) * 8) * TAU;

                        var dt = Math.Sin(alpha) * rB;
                        rA = (float)(1f / Math.Tan(alpha) * (d.X - dt) + d.Y - rB * Math.Sin(alpha));
                        cA = pointA + new Vector2(0, rA);

                        DrawArcTo(drawList, cB, rB, Pi / 2, Pi / 2 + alpha * shrinkArkAngle);
                        DrawArcTo(drawList, cA, rA, 1.5f * Pi + alpha * shrinkArkAngle, 1.5f * Pi);

                        if (exceededMaxRadius)
                            drawList.PathLineTo(pointAOrg);
                    }
                    else
                    {
                        rA = d.X - rB;
                        cA = pointA + new Vector2(0, rA);

                        DrawArcTo(drawList, cB, rB, 0.5f * Pi, Pi);
                        DrawArcTo(drawList, cA, rA, 2 * Pi, 1.5f * Pi);

                        if (exceededMaxRadius)
                            drawList.PathLineTo(pointAOrg);
                    }
                }
                else
                {
                    FnDrawBezierFallback();
                }
            }
            else
            {
                var cB = new Vector2(rectB.Min.X, rectB.Max.Y);
                var rB = (cB.Y - pointB.Y) * edgeFactor + edgeOffset;

                var exceededMaxRadius = d.X - rB > maxRadius;
                if (exceededMaxRadius)
                {
                    pointA.X += d.X - maxRadius - rB;
                    d.X = maxRadius + rB;
                }

                var cA = new Vector2(rectA.Max.X, rectA.Min.Y);
                var rA = pointA.Y - cA.Y;
                cB.Y = pointB.Y + rB;

                if (d.X > rA + rB)
                {
                    var horizontalStretch = Math.Abs(d.Y) < 2 || -d.X / d.Y > 1;
                    if (horizontalStretch)
                    {
                        // hack to find angle where circles touch
                        var f = -d.Y / d.X;
                        var alpha = (float)(f * 90 + Math.Sin(f * 3.1415f) * 8f) * TAU;

                        var dt = Math.Sin(alpha) * rB;
                        rA = (float)(1f / Math.Tan(alpha) * (d.X - dt) - d.Y - rB * Math.Sin(alpha));
                        cA = pointA - new Vector2(0, rA);

                        DrawArcTo(drawList, cB, rB, 1.5f * Pi, 1.5f * Pi - alpha * shrinkArkAngle);
                        DrawArcTo(drawList, cA, rA, 0.5f * Pi - alpha * shrinkArkAngle, 0.5f * Pi);

                        
                        if (exceededMaxRadius)
                            drawList.PathLineTo(pointAOrg);

                    }
                    else
                    {
                        rA = d.X - rB;
                        cA = pointA - new Vector2(0, rA);
                        
                        DrawArcTo(drawList, cB, rB, 1.5f * Pi, Pi);
                        DrawArcTo(drawList, cA, rA, 2 * Pi, 2.5f * Pi);
                        
                        if (exceededMaxRadius)
                            drawList.PathLineTo(pointAOrg);
                    }
                }
                else
                {
                    FnDrawBezierFallback();
                }
            }


            var isHovering =                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          TestHover(ref drawList, ref hoverPosition );
            if (currentCanvasScale > 0.5f)
            {
                drawList.AddPolyline(ref drawList._Path[0], drawList._Path.Size,   ColorVariations.OperatorOutline.Apply(color), ImDrawFlags.None, thickness + outlineWidth);
            }

            drawList.PathStroke(color, ImDrawFlags.None, thickness);
            return isHovering;
            
            void FnDrawBezierFallback()
            {
                var tangentLength = MathUtils.RemapAndClamp(Vector2.Distance(pointA, pointB),
                                                    30, 300,
                                                    5, 200);
                drawList.PathLineTo(pointA);
                drawList.PathBezierCubicCurveTo(pointA + new Vector2(tangentLength, 0),
                                           pointB + new Vector2(-tangentLength, 0),
                                           pointB,
                                           currentCanvasScale < 0.5f ? 11 : 29
                                          );
            }
        }

        private static void DrawArcTo(ImDrawListPtr drawList, Vector2 center, float radius, float angleA, float angleB)
        {
            if (radius < 0.01)
                return;
            
            drawList.PathArcTo(center, radius, angleA, angleB, radius< 50? 4:10);
        }

        public static bool TestHover(ref ImDrawListPtr drawList, ref Vector2 positionOnLine)
        {
            var foreground = ImGui.GetForegroundDrawList();
            if (drawList._Path.Size < 2)
                return false;

            const float distance = 6;

            var p1 = drawList._Path[0];
            var p2 = drawList._Path[drawList._Path.Size - 1];
            //foreground.AddRect(p1, p2, Color.Orange);
            var r = new ImRect(p2, p1).MakePositive();

            r.Expand(distance);
            var mousePos = ImGui.GetMousePos();
            if (!r.Contains(mousePos))
                return false;

            var pLast = p1;
            for (int i = 1; i < drawList._Path.Size; i++)
            {
                var p = drawList._Path[i];

                r = new ImRect(pLast, p).MakePositive();
                r.Expand(distance);
                //foreground.AddRect(r.Min, r.Max, Color.Gray);
                if (r.Contains(mousePos))
                { 
                    //foreground.AddRect(r.Min, r.Max, Color.Orange);
                    var v = (pLast - p);
                    var vLen = v.Length();
                    
                    var d = Vector2.Dot(v, mousePos-p) / vLen;
                    positionOnLine = p + v * d/vLen;
                    //foreground.AddCircleFilled(positionOnLine, 4f, Color.Red);
                    if (Vector2.Distance(mousePos, positionOnLine) <= distance)
                    {
                        return true;
                    }

                    return false;
                }

                pLast = p;
            }

            return false;
        }
    }
}