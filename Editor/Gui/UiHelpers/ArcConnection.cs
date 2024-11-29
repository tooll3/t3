using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Utils;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.UiHelpers;

public static class GraphConnectionDrawer
{
    private const float Pi = (float)Math.PI;

    public static bool DrawConnection(Vector2 canvasScale, ImRect Sn, Vector2 Sp,
                                      ImRect Tn, Vector2 Tp, uint color, float thickness,
                                      ref Vector2 hoverPosition)
    {
        var currentCanvasScale = Clamp(canvasScale.X, 0.2f, 2f);

        // Early out if not visible
        var r2 = Sn;
        r2.Add(Tn);
        r2.Add(Tp);
        r2.Add(Sp);

        if (!ImGui.IsRectVisible(r2.Min, r2.Max))
            return false;

        var drawList = ImGui.GetWindowDrawList();
        drawList.PathClear();

        // Ensure rects have valid sizes
        var fallbackRectSize = new Vector2(120, 50) * currentCanvasScale;
        if (Sn.GetHeight() < 1)
        {
            var rectAMin = new Vector2(Sp.X - fallbackRectSize.X, Sp.Y - fallbackRectSize.Y / 2);
            Sn = new ImRect(rectAMin, rectAMin + fallbackRectSize);
        }

        if (Tn.GetHeight() < 1)
        {
            var rectBMin = new Vector2(Tp.X, Tp.Y - fallbackRectSize.Y / 2);
            Tn = new ImRect(rectBMin, rectBMin + fallbackRectSize);
        }

        float dx = Tp.X - Sp.X;
        float dy = Tp.Y - Sp.Y;

        bool sourceAboveTarget = Sp.Y < Tp.Y;

        // Determine radii
        float Sc_r, Tc_r;
        if (sourceAboveTarget)
        {
            Sc_r = Sn.Max.Y - Sp.Y; // Distance to bottom of source node
            Tc_r = Tp.Y - Tn.Min.Y; // Distance from target point to top of target node
        }
        else
        {
            Sc_r = Sp.Y - Sn.Min.Y; // Distance to top of source node
            Tc_r = Tn.Max.Y - Tp.Y; // Distance from target point to bottom of target node
        }

        Tc_r *= 0.2f;

        var d = new Vector2(dx, dy).Length();
        var minTargetRadius = MathF.Min(d * 0.1f, 20);

        Tc_r = MathF.Max(Tc_r, minTargetRadius * canvasScale.X);
        Tc_r = MathF.Max(Tc_r, minTargetRadius);
        
        
        float possibleSourceRadius = MathF.Abs( dx - Tc_r);
        float clampedSourceRadius = MathF.Min(possibleSourceRadius, UserSettings.Config.MaxCurveRadius * canvasScale.X);
        Sc_r = MathF.Max(clampedSourceRadius, Sc_r);

        float sumR = Sc_r + Tc_r;

        // Adjust Sc.x to be further left by Sc_r
        float Sc_x = Sp.X + dx - Tc_r - Sc_r;
        if (dx < sumR)
        {
            // If horizontal space is too small, adjust Sc_x to Sp.X
            Sc_x = Sp.X;
        }

        float Tc_x = Tp.X;
        float Sc_y, Tc_y;

        if (sourceAboveTarget)
        {
            Sc_y = Sp.Y + Sc_r;
            Tc_y = Tp.Y - Tc_r;
        }
        else
        {
            Sc_y = Sp.Y - Sc_r;
            Tc_y = Tp.Y + Tc_r;
        }

        Vector2 Sc = new Vector2(Sc_x, Sc_y);
        Vector2 Tc = new Vector2(Tc_x, Tc_y);

        drawList.AddCircle(Sc, Sc_r, Color.Orange.Fade(0.1f));
        drawList.AddCircle(Tc, Tc_r, Color.Orange.Fade(0.1f));

        // Build the path
        drawList.PathLineTo(Sp); // Start at source point
        //drawList.PathLineTo(new Vector2(Sc_x, Sp.Y));  // Horizontal line to (Sc_x, Sp.Y)

        // Determine angles for arcs
        float startAngle_Sc, endAngle_Sc;
        float startAngle_Tc, endAngle_Tc;

        if (dx >= sumR && MathF.Abs(dy) > sumR)
        {
            // Ideal case, use fixed angles
            if (sourceAboveTarget)
            {
                // Source Arc from 270 degrees to 360 degrees
                startAngle_Sc = 1.5f * Pi;
                endAngle_Sc = 2f * Pi;

                // Target Arc from 180 degrees to 90 degrees
                startAngle_Tc = Pi;
                endAngle_Tc = 0.5f * Pi;
            }
            else
            {
                // Source Arc from 90 degrees to 0 degrees
                startAngle_Sc = 0.5f * Pi;
                endAngle_Sc = 0f;

                // Target Arc from 180 degrees to 270 degrees
                startAngle_Tc = Pi;
                endAngle_Tc = 1.5f * Pi;
            }
        }
        else
        {
            float distanceBetweenCenters = Vector2.Distance(Sc, Tc);

            if (distanceBetweenCenters < Math.Abs(Sc_r - Tc_r))
            {
                // Circles are overlapping; draw a straight line for simplicity
                drawList.PathLineTo(Tp);
                drawList.PathStroke(color, ImDrawFlags.None, thickness);
                return false;
            }

            if (MathF.Abs(dy) < sumR)
            {
                // Vertical space is too small, adjust the start and end angles
                var flipped = Sc_y > Tc_y 
                              //&& Sc_x + Sc_r < Tc_x - Tc_r
                              ;
                              //
                //flipped = false;
                //flipped = true;
                
                
                float angleAdjustment = ComputeInnerTangentAngle(Sc, Sc_r-1, Tc, Tc_r, flipped);
                if (sourceAboveTarget)
                {
                    // Adjust angles for source arc
                    startAngle_Sc = 1.5f * Pi;
                    //endAngle_Sc = angleBetweenCenters + angleAdjustment;
                    endAngle_Sc = 2 * Pi + angleAdjustment;

                    // Adjust angles for target arc
                    //startAngle_Tc = angleBetweenCenters + angleAdjustment;
                    startAngle_Tc = 1f * Pi + angleAdjustment;
                    endAngle_Tc = 0.5f * Pi;
                }
                else
                {
                    // Adjust angles for source arc
                    startAngle_Sc = 0.5f * Pi;
                    //endAngle_Sc = angleBetweenCenters - angleAdjustment;
                    endAngle_Sc = +angleAdjustment;

                    // Adjust angles for target arc
                    //startAngle_Tc = angleBetweenCenters - angleAdjustment;
                    startAngle_Tc = Pi + angleAdjustment;
                    endAngle_Tc = 1.5f * Pi;
                }
            }
            else
            {
                // Horizontal space is too small, adjust the start and end angles
                var flipped = Sc_x - Sc_r < Tc_x + Tc_r 
                              || Sc_y + Sc_r > Tc_y - Tc_r;

                float angleAdjustment = ComputeInnerTangentAngle(Sc, Sc_r, Tc, Tc_r, flipped);
                if (sourceAboveTarget)
                {
                    // Adjust angles for source arc
                    startAngle_Sc = 1.5f * Pi;
                    //endAngle_Sc = angleBetweenCenters + angleAdjustment;
                    endAngle_Sc = 2 * Pi + angleAdjustment;

                    // Adjust angles for target arc
                    //startAngle_Tc = angleBetweenCenters + angleAdjustment;
                    startAngle_Tc = 1f * Pi + angleAdjustment;
                    endAngle_Tc = 0.5f * Pi;
                }
                else
                {
                    // Adjust angles for source arc
                    startAngle_Sc = 0.5f * Pi;
                    //endAngle_Sc = angleBetweenCenters - angleAdjustment;
                    endAngle_Sc = +angleAdjustment;

                    // Adjust angles for target arc
                    //startAngle_Tc = angleBetweenCenters - angleAdjustment;
                    startAngle_Tc = Pi + angleAdjustment;
                    endAngle_Tc = 1.5f * Pi;
                }
            }
        }

        // Draw source arc
        drawList.PathArcTo(Sc, Sc_r, startAngle_Sc, endAngle_Sc, 10);

        // If arcs are connected via inner tangent, no vertical line is needed
        if (dx >= sumR)
        {
            // Vertical line from end of source arc to start of target arc
            //drawList.PathLineTo(new Vector2(Sc_x, Tc_y));
        }

        // Draw target arc
        drawList.PathArcTo(Tc, Tc_r, startAngle_Tc, endAngle_Tc, 10);

        drawList.PathLineTo(Tp); // Line to target point

        // Finalize drawing
        var isHovering = TestHover(drawList, ref hoverPosition);

        if (currentCanvasScale > 0.5f)
        {
            // Optionally draw an outline
            drawList.AddPolyline(ref drawList._Path[0], drawList._Path.Size, AdjustColor(color), ImDrawFlags.None, thickness + 3f);
        }

        drawList.PathStroke(color, ImDrawFlags.None, thickness);

        return isHovering;
    }

    private static float Clamp(float value, float min, float max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    private static uint AdjustColor(uint color)
    {
        // Implement color adjustment if needed
        return color;
    }

    private static bool TestHover(ImDrawListPtr drawList, ref Vector2 hoverPosition)
    {
        // Implement hover detection if needed
        return false;
    }

    public static float ComputeInnerTangentAngle(Vector2 centerA, float radiusA, Vector2 centerB, float radiusB, bool flipped = false)
    {
        // Calculate the differences in x and y coordinates
        double deltaX = centerB.X - centerA.X;
        double deltaY = centerB.Y - centerA.Y;

        // Calculate the distance between the centers of the circles
        double d = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

        // Check if the inner tangent exists
        if (d <= Math.Abs(radiusA - radiusB))
        {
            return 0;
            //throw new Exception("No inner tangent exists since the circles overlap or one is contained within the other.");
        }

        // Base angle between the centers
        double thetaBase = Math.Atan2(deltaY, deltaX);

        // Angle offset for the inner tangent
        double thetaOffset = Math.Acos((radiusA + radiusB) / d);

        // Calculate both possible angles of the inner tangents
        double angle1 = NormalizeAngle(thetaBase + thetaOffset);
        double angle2 = NormalizeAngle(thetaBase - thetaOffset);

        // Decide which angle to use based on the position of Circle B relative to Circle A
        double selectedAngle;

        if (flipped)
        {
            // B is below A, choose the angle that points downward
            selectedAngle = (angle1 < 0) ? angle1 : angle2;
        }
        else
        {
            // B is above A, choose the angle that points upward
            selectedAngle = (angle1 > 0) ? angle1 : angle2;
        }

        return (float)selectedAngle;
    }

    public static double NormalizeAngle(double angle)
    {
        while (angle <= -Math.PI) angle += 2 * Math.PI;
        while (angle > Math.PI) angle -= 2 * Math.PI;
        return angle;
    }
}

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

    public static bool Draw(Vector2 canvasScale, ImRect rectA, Vector2 pointA, ImRect rectB, Vector2 pointB, Color color, float thickness,
                            ref Vector2 hoverPosition)
    {
        var currentCanvasScale = canvasScale.X.Clamp(0.2f, 2f);
        pointA.X -= (3 - 1 * currentCanvasScale).Clamp(1, 1);
        pointB.X += (4 - 4 * currentCanvasScale).Clamp(0, 4);

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
        var maxRadius = limitArcConnectionRadius * currentCanvasScale * 2f;
        const float shrinkArkAngle = 0.8f;
        var edgeFactor = (0.35f * (currentCanvasScale - 0.3f)).Clamp(0.01f, 0.35f); // 0 -> overlap  ... 1 concentric around node edge
        const float outlineWidth = 3;
        var edgeOffset = 1 * currentCanvasScale;

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

        var isHovering = TestHover(ref drawList, ref hoverPosition);
        if (currentCanvasScale > 0.5f)
        {
            drawList.AddPolyline(ref drawList._Path[0], drawList._Path.Size, ColorVariations.OperatorOutline.Apply(color), ImDrawFlags.None,
                                 thickness + outlineWidth);
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

        drawList.PathArcTo(center, radius, angleA, angleB, radius < 50 ? 4 : 10);
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

                var d = Vector2.Dot(v, mousePos - p) / vLen;
                positionOnLine = p + v * d / vLen;
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