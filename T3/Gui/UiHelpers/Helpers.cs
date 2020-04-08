using ImGuiNET;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using T3.Gui;
using T3.Gui.Graph;
using T3.Gui.UiHelpers;
using T3.Gui.Windows;
using T3.Gui.Windows.TimeLine;

namespace UiHelpers
{
    /// <summary>
    /// A collection of helper and debug function for IMGUI development
    /// </summary>
    static class THelpers
    {
        public static ImDrawListPtr OutlinedRect(ref ImDrawListPtr drawList, Vector2 position, Vector2 size, uint fill, uint outline, float cornerRadius = 4)
        {
            drawList.AddRectFilled(position, position + size, fill, cornerRadius);
            drawList.AddRect(position, position + size, outline, cornerRadius);
            return drawList;
        }

        public static ImDrawListPtr OutlinedRect(ref ImDrawListPtr drawList, Vector2 position, Vector2 size, Color fill, Color outline, float cornerRadius = 4)
        {
            drawList.AddRectFilled(position, position + size, fill, cornerRadius);
            drawList.AddRect(position, position + size, outline, cornerRadius);
            return drawList;
        }

        /// <summary>
        /// Draws an overlay rectangle in screen space
        /// </summary>
        public static void DebugRect(Vector2 screenMin, Vector2 screenMax, string label = "")
        {
            var overlayDrawlist = ImGui.GetForegroundDrawList();
            overlayDrawlist.AddRect(screenMin, screenMax, Color.Green);
            overlayDrawlist.AddText(new Vector2(screenMin.X, screenMax.Y), Color.Green, label);
        }

        public static void DebugRect(Vector2 screenMin, Vector2 screenMax, Color color, string label = "")
        {
            var overlayDrawlist = ImGui.GetForegroundDrawList();
            overlayDrawlist.AddRect(screenMin, screenMax, color);
            overlayDrawlist.AddText(new Vector2(screenMin.X, screenMax.Y), color, label);
        }

        /// <summary>
        /// Draws an outline of the current (last) Imgui item
        /// </summary>
        public static void DebugItemRect(string label = "", uint color = 0xff20ff80)
        {
            if (SettingsWindow.ItemRegionsVisible)
                DebugRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), color, label);
        }

        public static void DebugWindowRect(string label = "", uint color = 0xffff2080)
        {
            if (SettingsWindow.WindowRegionsVisible)
                DebugRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), color, label);
        }
    }

    /// <summary>
    /// 2D axis aligned bounding-box. It's a port of IMGUIs internal class.
    /// FIXME: this should be replaced with a .net Rect-Class
    /// </summary>
    public struct ImRect
    {
        public Vector2 Min; // Upper-left
        public Vector2 Max; // Lower-right

        public ImRect(Vector2 min, Vector2 max)
        {
            Min = min;
            Max = max;
        }

        public ImRect(Vector4 v)
        {
            Min = new Vector2(v.X, v.Y);
            Max = new Vector2(v.Z, v.W);
        }

        public ImRect(float x1, float y1, float x2, float y2)
        {
            Min = new Vector2(x1, y1);
            Max = new Vector2(x2, y2);
        }

        public Vector2 GetCenter()
        {
            return new Vector2((Min.X + Max.X) * 0.5f, (Min.Y + Max.Y) * 0.5f);
        }

        public Vector2 GetSize()
        {
            return new Vector2(Max.X - Min.X, Max.Y - Min.Y);
        }

        public float GetWidth()
        {
            return Max.X - Min.X;
        }

        public float GetHeight()
        {
            return Max.Y - Min.Y;
        }

        /// <summary>
        /// Top-left
        /// </summary>
        public Vector2 GetTL()
        {
            return Min;
        }

        /// <summary>
        /// Top right
        /// </summary>
        public Vector2 GetTR()
        {
            return new Vector2(Max.X, Min.Y);
        }

        /// <summary>
        /// Bottom left
        /// </summary>
        public Vector2 GetBL()
        {
            return new Vector2(Min.X, Max.Y);
        }

        /// <summary>
        /// Bottom right
        /// </summary>
        public Vector2 GetBR()
        {
            return Max;
        }

        public bool Contains(Vector2 p)
        {
            return p.X >= Min.X && p.Y >= Min.Y && p.X < Max.X && p.Y < Max.Y;
        }

        public bool Contains(ImRect r)
        {
            return r.Min.X >= Min.X && r.Min.Y >= Min.Y && r.Max.X <= Max.X && r.Max.Y <= Max.Y;
        }

        public bool Overlaps(ImRect r)
        {
            return r.Min.Y < Max.Y && r.Max.Y > Min.Y && r.Min.X < Max.X && r.Max.X > Min.X;
        }

        public void Add(Vector2 p)
        {
            if (Min.X > p.X) Min.X = p.X;
            if (Min.Y > p.Y) Min.Y = p.Y;
            if (Max.X < p.X) Max.X = p.X;
            if (Max.Y < p.Y) Max.Y = p.Y;
        }

        public void Add(ImRect r)
        {
            if (Min.X > r.Min.X) Min.X = r.Min.X;
            if (Min.Y > r.Min.Y) Min.Y = r.Min.Y;
            if (Max.X < r.Max.X) Max.X = r.Max.X;
            if (Max.Y < r.Max.Y) Max.Y = r.Max.Y;
        }

        public void Expand(float amount)
        {
            Min.X -= amount;
            Min.Y -= amount;
            Max.X += amount;
            Max.Y += amount;
        }

        public void Expand(Vector2 amount)
        {
            Min.X -= amount.X;
            Min.Y -= amount.Y;
            Max.X += amount.X;
            Max.Y += amount.Y;
        }

        public void Translate(Vector2 d)
        {
            Min.X += d.X;
            Min.Y += d.Y;
            Max.X += d.X;
            Max.Y += d.Y;
        }

        public void TranslateX(float dx)
        {
            Min.X += dx;
            Max.X += dx;
        }

        public void TranslateY(float dy)
        {
            Min.Y += dy;
            Max.Y += dy;
        }

        public static ImRect RectBetweenPoints(Vector2 a, Vector2 b)
        {
            return new ImRect(
                              x1: Im.Min(a.X, b.X),
                              y1: Im.Min(a.Y, b.Y),
                              x2: Im.Max(a.X, b.X),
                              y2: Im.Max(a.Y, b.Y));
        }

        public static ImRect RectWithSize(Vector2 position, Vector2 size)
        {
            return new ImRect(position, position + size);
        }

        // Simple version, may lead to an inverted rectangle, which is fine for Contains/Overlaps test but not for display.
        public void ClipWith(ImRect r)
        {
            Min = Im.Max(Min, r.Min);
            Max = Im.Min(Max, r.Max);
        }

        // Full version, ensure both points are fully clipped.
        public void ClipWithFull(ImRect r)
        {
            Min = Im.Clamp(Min, r.Min, r.Max);
            Max = Im.Clamp(Max, r.Min, r.Max);
        }

        public void Floor()
        {
            Min.X = (float)(int)Min.X;
            Min.Y = (float)(int)Min.Y;
            Max.X = (float)(int)Max.X;
            Max.Y = (float)(int)Max.Y;
        }

        bool IsInverted()
        {
            return Min.X > Max.X || Min.Y > Max.Y;
        }

        public override string ToString()
        {
            return $"Rect {Min}  {Max}";
        }

        public float GetAspect()
        {
            return GetWidth() / GetHeight();
        }
    }

    /// <summary>
    /// Manual port of helper-functions defined in imgui_internal.h
    /// </summary>
    static class Im
    {
        public static Vector2 Min(Vector2 lhs, Vector2 rhs)
        {
            return new Vector2(lhs.X < rhs.X ? lhs.X : rhs.X, lhs.Y < rhs.Y ? lhs.Y : rhs.Y);
        }

        public static Vector2 Floor(Vector2 v)
        {
            return new Vector2((float)Math.Floor(v.X), (float)Math.Floor(v.Y));
        }

        public static Vector2 Max(Vector2 lhs, Vector2 rhs)
        {
            return new Vector2(lhs.X >= rhs.X ? lhs.X : rhs.X, lhs.Y >= rhs.Y ? lhs.Y : rhs.Y);
        }

        public static Vector2 Clamp(Vector2 v, Vector2 mn, Vector2 mx)
        {
            return new Vector2((v.X < mn.X)
                                   ? mn.X
                                   : (v.X > mx.X)
                                       ? mx.X
                                       : v.X, (v.Y < mn.Y) ? mn.Y : (v.Y > mx.Y) ? mx.Y : v.Y);
        }

        public static T Min<T>(T lhs, T rhs) where T : System.IComparable<T>
        {
            return lhs.CompareTo(rhs) < 0 ? lhs : rhs;
        }

        public static T Max<T>(T lhs, T rhs) where T : System.IComparable<T>
        {
            return lhs.CompareTo(rhs) >= 0 ? lhs : rhs;
        }

        public static T Clamp<T>(this T val, T min, T max) where T : System.IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        public static float Lerp(float a, float b, float t)
        {
            return (float)(a + (b - a) * t);
        }

        public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            return new Vector2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        }

        public static double Lerp(double a, double b, double t)
        {
            return (double)(a + (b - a) * t);
        }

        public static int Lerp(int a, int b, float t)
        {
            return (int)(a + (b - a) * t);
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            T tmp = a;
            a = b;
            b = tmp;
        }

        public static float Fmod(float v, float mod)
        {
            return v - mod * (float)Math.Floor(v / mod);
        }

        public static double Fmod(double v, double mod)
        {
            return v - mod * Math.Floor(v / mod);
        }

        public static float Remap(float value, float inMin, float inMax, float outMin, float outMax)
        {
            var factor = (value - inMin) / (inMax - inMin);
            var v = factor * (outMax - outMin) + outMin;
            if (outMin > outMax)
                Swap(ref outMin, ref outMax);
            return v.Clamp(outMin, outMax);
        }

        public static double Remap(double value, double inMin, double inMax, double outMin, double outMax)
        {
            var factor = (value - inMin) / (inMax - inMin);
            var v = factor * (outMax - outMin) + outMin;
            if (v > outMax)
            {
                v = outMax;
            }
            else if (v < outMin)
            {
                v = outMin;
            }

            return v;
        }

        public static void DrawSplitter(bool splitVertically, float thickness, ref float size0, ref float size1, float minSize0, float minSize1)
        {
            var backupPos = ImGui.GetCursorPos();
            if (splitVertically)
                ImGui.SetCursorPosY(backupPos.Y + size0);
            else
                ImGui.SetCursorPosX(backupPos.X + size0);

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 1));

            // We don't draw while active/pressed because as we move the panes the splitter button will be 1 frame late
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0, 0, 0, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.6f, 0.6f, 0.10f));
            ImGui.Button("##Splitter", new Vector2(!splitVertically ? thickness : -1.0f, splitVertically ? thickness : -1.0f));
            ImGui.PopStyleColor(3);

            ImGui.SetItemAllowOverlap(); // This is to allow having other buttons OVER our splitter. 

            if (ImGui.IsAnyItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
            }

            if (ImGui.IsItemActive())
            {
                var mouseDelta = splitVertically ? ImGui.GetIO().MouseDelta.Y : ImGui.GetIO().MouseDelta.X;

                //// Minimum pane size
                //if (mouse_delta < min_size0 - size0)
                //    mouse_delta = min_size0 - size0;
                //if (mouse_delta > size1 - min_size1)
                //    mouse_delta = size1 - min_size1;

                // Apply resize
                size0 += mouseDelta;
                size1 -= mouseDelta;
            }

            ImGui.SetCursorPos(backupPos);
        }

        public static void DrawContentRegion()
        {
            ImGui.GetForegroundDrawList().AddRect(
                                                  ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos(),
                                                  ImGui.GetWindowContentRegionMax() + ImGui.GetWindowPos(),
                                                  Color.White);
        }

        public static void ToggleButton(string str_id, ref bool v)
        {
            var p = ImGui.GetCursorScreenPos();
            var drawList = ImGui.GetWindowDrawList();

            var height = ImGui.GetFrameHeight();
            var width = height * 1.55f;
            var radius = height * 0.50f;

            ImGui.InvisibleButton(str_id, new Vector2(width, height));
            if (ImGui.IsItemClicked())
                v = !v;

            var t = v ? 1.0f : 0.0f;

            //ImGuiContext & g = *GImGui;
            //var g = ImGui.GetCurrentContext();
            //float ANIM_SPEED = 0.08f;
            //if (g.LastActiveId == g.CurrentWindow->GetID(str_id))// && g.LastActiveIdTimer < ANIM_SPEED)
            //{
            //    float t_anim = ImSaturate(g.LastActiveIdTimer / ANIM_SPEED);
            //    t = v ? (t_anim) : (1.0f - t_anim);
            //}

            var colBg = ImGui.IsItemHovered()
                            ? Color.White
                            : Color.Red;

            drawList.AddRectFilled(p, new Vector2(p.X + width, p.Y + height), colBg, height * 0.5f);
            drawList.AddCircleFilled(new Vector2(p.X + radius + t * (width - radius * 2.0f), p.Y + radius), radius - 1.5f, Color.White);
        }

        public static void EmptyWindowMessage(string message)
        {
            var center = (ImGui.GetWindowContentRegionMax() + ImGui.GetWindowContentRegionMin()) / 2;
            var textSize = ImGui.CalcTextSize(message);
            center -= textSize * 0.5f;
            ImGui.SetCursorScreenPos(ImGui.GetWindowPos() + center);

            ImGui.PushStyleColor(ImGuiCol.Text, new Color(0.4f).Rgba);
            ImGui.Text(message);
            ImGui.PopStyleColor();
        }

        /// <summary>
        /// Draws an arc connection line.
        /// </summary>
        /// <remarks>
        /// Assumes that B is the tight node connection direction is bottom to top
        /// </remarks>
        ///
        ///
        ///
        /*                                 dx
         *
         *        +-------------------+
         *        |      A            +----\  rB+---------------+
         *        +-------------------+rA   \---+      B        |
          *                                     +---------------+
         */
        private const float Pi = 3.141578f;

        private const float TAU = Pi / 180;
        private static readonly Color OutlineColor = new Color(0.1f, 0.1f, 0.1f, 0.6f);

        public static void DrawArcConnection(ImRect rectA, Vector2 pointA, ImRect rectB, Vector2 pointB, Color color, float thickness)
        {
            var drawList = ImGui.GetWindowDrawList();

            var fallbackRectSize = new Vector2(120, 50) * GraphCanvas.Current.Scale;
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
            // THelpers.DebugRect(rectB.Min, rectB.Max, Color.Orange);

            var d = pointB - pointA;

            var maxRadius = SettingsWindow.LimitArcConnectionRadius * GraphCanvas.Current.Scale.X;
            const float shrinkArkAngle = 0.8f;
            const float edgeFactor = 0.2f; // 0 -> overlap  ... 1 concentric around node edge
            const float outlineWidth = 3;
            var edgeOffset = 10 * GraphCanvas.Current.Scale.X;
            
            var pointAOrg = pointA;

            if (d.Y > -1 && d.Y < 1 && d.X > 2)
            {
                drawList.AddLine(pointA, pointB, color,thickness);
                return;
            }
            
            var aAboveB = d.Y > 0;
            if (aAboveB)
            {
                var cB = rectB.Min;
                var rB = (pointB.Y - cB.Y) * edgeFactor + edgeOffset;
                
                var exceededMaxRadius = d.X - rB > maxRadius;
                if (exceededMaxRadius)
                {
                    pointA.X += d.X - maxRadius - rB;
                    d.X = maxRadius +rB;
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

                        drawList.PathClear();
                        drawList.PathArcTo(cB, rB, Pi / 2, Pi / 2 + alpha * shrinkArkAngle);
                        drawList.PathArcTo(cA, rA, 1.5f * Pi + alpha * shrinkArkAngle, 1.5f * Pi);
                        if(exceededMaxRadius)
                            drawList.PathLineTo(pointAOrg);
                        
                        drawList.AddPolyline(ref drawList._Path[0], drawList._Path.Size, OutlineColor, false, thickness+outlineWidth);
                        drawList.PathStroke(color, false, thickness);
                        
                        // drawList.AddLine(pointA, pointB, Color.Red);
                        // drawList.AddCircle(cB, rB, Color.Red);
                        // drawList.AddCircle(cA, rA, new Color(1f,0,0,0.2f));
                        // drawList.AddRect(rectA.Min, rectA.Max, Color.Red);
                        // drawList.AddRect(rectB.Min, rectB.Max, Color.Green);
                    }
                    else
                    {
                        rA = d.X - rB;
                        cA = pointA + new Vector2(0, rA);

                        drawList.PathClear();
                        drawList.PathArcTo(cB, rB, 0.5f * Pi, Pi);
                        drawList.PathArcTo(cA, rA, 2 * Pi, 1.5f * Pi);
                        if(exceededMaxRadius)
                            drawList.PathLineTo(pointAOrg);
                        drawList.AddPolyline(ref drawList._Path[0], drawList._Path.Size, OutlineColor, false, thickness+outlineWidth);
                        drawList.PathStroke(color, false, thickness);
                    }
                }
                else
                {
                    DrawBezierFallback();
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
                    d.X = maxRadius +rB;
                }
                
                var cA = new Vector2(rectA.Max.X, rectA.Min.Y);
                var rA = pointA.Y - cA.Y;
                cB.Y = pointB.Y + rB;

                
                
                if (d.X > rA + rB)
                {
                    var horizontalStretch = Math.Abs(d.Y) <2 || -d.X / d.Y > 1;
                    if (horizontalStretch)
                    {
                        // hack to find angle where circles touch
                        var f = -d.Y / d.X;
                        var alpha = (float)(f * 90 + Math.Sin(f * 3.1415f) * 8f) * TAU;

                        var dt = Math.Sin(alpha) * rB;
                        rA = (float)(1f / Math.Tan(alpha) * (d.X - dt) - d.Y - rB * Math.Sin(alpha));
                        cA = pointA - new Vector2(0, rA);

                        drawList.PathClear();
                        drawList.PathArcTo(cB, rB, 1.5f * Pi, 1.5f * Pi - alpha* shrinkArkAngle);
                        drawList.PathArcTo(cA, rA, 0.5f * Pi - alpha* shrinkArkAngle, 0.5f * Pi);
                        if(exceededMaxRadius)
                            drawList.PathLineTo(pointAOrg);
                        drawList.AddPolyline(ref drawList._Path[0], drawList._Path.Size, OutlineColor, false, thickness+outlineWidth);
                        drawList.PathStroke(color, false, thickness);

                        
                    }
                    else
                    {
                        rA = d.X - rB;
                        cA = pointA - new Vector2(0, rA);

                        drawList.PathClear();
                        drawList.PathArcTo(cB, rB, 1.5f * Pi, Pi);
                        drawList.PathArcTo(cA, rA, 2 * Pi, 2.5f * Pi);
                        if(exceededMaxRadius)
                            drawList.PathLineTo(pointAOrg);
                        
                        drawList.AddPolyline(ref drawList._Path[0], drawList._Path.Size, OutlineColor, false, thickness+outlineWidth);
                        drawList.PathStroke(color, false, thickness);
                        
                        // drawList.AddLine(pointA, pointB, Color.Red);
                        // drawList.AddCircle(cB, rB, Color.Red,128);
                        // drawList.AddCircle(cA, rA, new Color(1f, 0, 0, 0.2f), 128);
                        // drawList.AddText(pointA, Color.Gray, $"rA {rA}  rB {rB}  a {alpha}");
                    }
                }
                else
                {
                    DrawBezierFallback();
                }
            }

            void DrawBezierFallback()
            {
                var tangentLength = Im.Remap(Vector2.Distance(pointA, pointB),
                                             30, 300,
                                             5, 200);
                drawList.AddBezierCurve(
                                        pointA,
                                        pointA + new Vector2(tangentLength, 0),
                                        pointB + new Vector2(-tangentLength, 0),
                                        pointB,
                                        color,
                                        thickness:  thickness,
                                        num_segments: 30);
            }
        }

        // // Flex Horizontal or vertical
        // var dx1 = rectA.Min.X - rectB.Max.X;
        // var dx2 = rectB.Max.X - rectA.Min.X;
        //
        // var dy = rectA.Min.Y - rectB.Max.Y;
        //
        // var dPx = pointA.X - pointB.X;
        //
        // var dPy = pointA.Y - pointB.Y;
        //     if (dx1 > 0)
        // {
        //     // B is left of A
        //     var cB = rectB.Max;
        //     var cA = new Vector2(rectA.Min.X, rectA.Min.Y);
        //     var rB = rectB.Max.X - pointB.X;
        //     var rMinA = pointA.X - rectA.Min.X;
        //
        //     var rMinB = rectB.Max.X - pointB.X;
        //     var rLimit = rB + rMinA;
        //
        //     ImGui.GetWindowDrawList().AddCircle(cA, rMinA, Color.Red);
        //     ImGui.GetWindowDrawList().AddCircle(cB, rMinB, Color.Green);
        //
        //     if (dy > rLimit && dPx > rLimit)
        //     {
        //         // Horizontal flex
        //         if (dPx > dPy)
        //         {
        //             var r2 = dy - rB;
        //             drawList.PathArcTo(cB, rB, 3.1415f, 1.5f);
        //             drawList.PathArcTo(cA + new Vector2(-r2 + rMinA, 0), r2, 1.5f * 3.1415f, 2f * 3.1415f);
        //             //drawList.PathLineTo(pointA);
        //             drawList.PathStroke(Color.White, false, 2);
        //         }
        //         // Vertical flex
        //         else
        //         {
        //             //drawList.PathLineTo(Vector2.Zero);
        //             var r2 = dPx - rB;
        //             drawList.PathArcTo(cB, rB, 3.1415f, 1.5f);
        //             drawList.PathArcTo(cA + new Vector2(-r2 + rMinA, -(dy - r2 - rB)), r2, 1.5f * 3.1415f, 2f * 3.1415f);
        //             drawList.PathLineTo(pointA);
        //             drawList.PathStroke(Color.White, false, 2);
        //         }
        //     }
        //     else
        //     {
        //         var cornerDistance = Vector2.Distance(cB, cA);
        //         if (cornerDistance < rMinA + rMinB)
        //         {
        //             // Min circles intersect -> reverse and use outer tangent
        //         }
        //         else
        //         {
        //             // Use inner tangent
        //             // ToDo: Compute angle θ -> https://stackoverflow.com/questions/49968720/find-tangent-points-in-a-circle-from-a-point/49987361#49987361
        //         }
        //     }
        // }
        //
        // else if (dx2 > 0)
        // {
        //     // A is left of B
        // }

        // public static void DrawArcConnection(ImRect rectA, Vector2 pointA, ImRect rectB, Vector2 pointB)
        // {
        //     var drawList = ImGui.GetWindowDrawList();
        //     drawList.AddRect(rectA.Min, rectA.Max, Color.Red);
        //     drawList.AddRect(rectB.Min, rectB.Max, Color.Green);
        //
        //     // Flex Horizontal or vertical
        //     var dx1 = rectA.Min.X - rectB.Max.X;
        //     var dx2 = rectB.Max.X - rectA.Min.X;
        //
        //     var dy = rectA.Min.Y - rectB.Max.Y;
        //
        //     var dPx = pointA.X - pointB.X;
        //     var dPy = pointA.Y - pointB.Y;
        //
        //     if (dx1 > 0)
        //     {
        //         // B is left of A
        //         var cB = rectB.Max;
        //         var cA = new Vector2(rectA.Min.X, rectA.Min.Y);
        //         var rB = rectB.Max.X - pointB.X;
        //         var rMinA = pointA.X - rectA.Min.X;
        //
        //         var rMinB = rectB.Max.X - pointB.X;
        //         var rLimit = rB + rMinA;
        //
        //         ImGui.GetWindowDrawList().AddCircle(cA, rMinA, Color.Red);
        //         ImGui.GetWindowDrawList().AddCircle(cB, rMinB, Color.Green);
        //
        //         if (dy > rLimit && dPx > rLimit)
        //         {
        //             // Horizontal flex
        //             if (dPx > dPy)
        //             {
        //                 var r2 = dy-rB;
        //                 drawList.PathArcTo(cB,rB, 3.1415f,1.5f);
        //                 drawList.PathArcTo(cA + new Vector2(-r2+rMinA, 0), r2, 1.5f*3.1415f, 2f*3.1415f);
        //                 //drawList.PathLineTo(pointA);
        //                 drawList.PathStroke(Color.White, false, 2);
        //             }
        //             // Vertical flex
        //             else
        //             {
        //                 //drawList.PathLineTo(Vector2.Zero);
        //                 var r2 = dPx-rB;
        //                 drawList.PathArcTo(cB,rB, 3.1415f,1.5f);
        //                 drawList.PathArcTo(cA + new Vector2(-r2+rMinA, -(dy-r2-rB)), r2, 1.5f*3.1415f, 2f*3.1415f);
        //                 drawList.PathLineTo(pointA);
        //                 drawList.PathStroke(Color.White, false, 2);
        //             }
        //
        //         }
        //         else
        //         {
        //             var cornerDistance = Vector2.Distance(cB, cA);
        //             if (cornerDistance < rMinA + rMinB)
        //             {
        //                 // Min circles intersect -> reverse and use outer tangent
        //             }
        //             else
        //             {
        //                 // Use inner tangent
        //                 // ToDo: Compute angle θ -> https://stackoverflow.com/questions/49968720/find-tangent-points-in-a-circle-from-a-point/49987361#49987361
        //             }
        //         }
        //     }
        //     else if (dx2 > 0)
        //     {
        //         // A is left of B
        //     }
        // }
    }
}