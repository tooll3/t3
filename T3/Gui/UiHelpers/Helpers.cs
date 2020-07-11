using ImGuiNET;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using T3.Core;
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

        public static void DebugContentRect(string label = "", uint color = 0xff804080)
        {
            if (!SettingsWindow.WindowRegionsVisible)
                return;

            var min = ImGui.GetWindowContentRegionMin();
            var max = ImGui.GetWindowContentRegionMax();
            DebugRect(ImGui.GetWindowPos() + min, ImGui.GetWindowPos() + max, color, label);
        }

        public static ImRect GetContentRegionArea()
        {
            return new ImRect(ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos(),
                              ImGui.GetWindowContentRegionMax() + ImGui.GetWindowPos());
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
        /// This is required before using <see cref="Contains(Vector2)"/>
        /// </summary>
        public ImRect MakePositive()
        {
            if (Min.X > Max.X)
            {
                var t = Min.X;
                Min.X = Max.X;
                Max.X = t;
            }

            if (Min.Y > Max.Y)
            {
                var t = Min.Y;
                Min.Y = Max.Y;
                Max.Y = t;
            }

            return this;
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

        /// <summary>
        /// This is required before using <see cref="Contains(Vector2)"/>
        /// </summary>
        /// <remarks>Please make sure to make the rectangle positive before testing</remarks>
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
                              x1: MathUtils.Min(a.X, b.X),
                              y1: MathUtils.Min(a.Y, b.Y),
                              x2: MathUtils.Max(a.X, b.X),
                              y2: MathUtils.Max(a.Y, b.Y));
        }

        public static ImRect RectWithSize(Vector2 position, Vector2 size)
        {
            return new ImRect(position, position + size);
        }

        // Simple version, may lead to an inverted rectangle, which is fine for Contains/Overlaps test but not for display.
        public void ClipWith(ImRect r)
        {
            Min = MathUtils.Max(Min, r.Min);
            Max = MathUtils.Min(Max, r.Max);
        }

        // Full version, ensure both points are fully clipped.
        public void ClipWithFull(ImRect r)
        {
            Min = MathUtils.Clamp(Min, r.Min, r.Max);
            Max = MathUtils.Clamp(Max, r.Min, r.Max);
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
        /// 
        ///                                dx
        ///        +-------------------+
        ///        |      A            +----\  rB+---------------+
        ///        +-------------------+rA   \---+      B        |
        ///                                      +---------------+
        /// </remarks>
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

            var d = pointB - pointA;

            var maxRadius = SettingsWindow.LimitArcConnectionRadius * GraphCanvas.Current.Scale.X;
            const float shrinkArkAngle = 0.8f;
            const float edgeFactor = 0.4f; // 0 -> overlap  ... 1 concentric around node edge
            const float outlineWidth = 3;
            var edgeOffset = 10 * GraphCanvas.Current.Scale.X;

            var pointAOrg = pointA;

            if (d.Y > -1 && d.Y < 1 && d.X > 2)
            {
                drawList.AddLine(pointA, pointB, color, thickness);
                return;
            }

            drawList.PathClear();
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

                        drawList.PathClear();
                        drawList.PathArcTo(cB, rB, Pi / 2, Pi / 2 + alpha * shrinkArkAngle);
                        drawList.PathArcTo(cA, rA, 1.5f * Pi + alpha * shrinkArkAngle, 1.5f * Pi);
                        if (exceededMaxRadius)
                            drawList.PathLineTo(pointAOrg);
                    }
                    else
                    {
                        rA = d.X - rB;
                        cA = pointA + new Vector2(0, rA);

                        drawList.PathArcTo(cB, rB, 0.5f * Pi, Pi);
                        drawList.PathArcTo(cA, rA, 2 * Pi, 1.5f * Pi);
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

                        drawList.PathArcTo(cB, rB, 1.5f * Pi, 1.5f * Pi - alpha * shrinkArkAngle);
                        drawList.PathArcTo(cA, rA, 0.5f * Pi - alpha * shrinkArkAngle, 0.5f * Pi);
                        if (exceededMaxRadius)
                            drawList.PathLineTo(pointAOrg);

                    }
                    else
                    {
                        rA = d.X - rB;
                        cA = pointA - new Vector2(0, rA);

                        drawList.PathArcTo(cB, rB, 1.5f * Pi, Pi);
                        drawList.PathArcTo(cA, rA, 2 * Pi, 2.5f * Pi);
                        if (exceededMaxRadius)
                            drawList.PathLineTo(pointAOrg);
                    }
                }
                else
                {
                    FnDrawBezierFallback();
                }
            }

            //TestHover(ref drawList);
            drawList.AddPolyline(ref drawList._Path[0], drawList._Path.Size, OutlineColor, false, thickness + outlineWidth);
            drawList.PathStroke(color, false, thickness);
            
            void FnDrawBezierFallback()
            {
                var tangentLength = MathUtils.Remap(Vector2.Distance(pointA, pointB),
                                                    30, 300,
                                                    5, 200);
                drawList.PathLineTo(pointA);
                drawList.PathBezierCurveTo(pointA + new Vector2(tangentLength, 0),
                                           pointB + new Vector2(-tangentLength, 0),
                                           pointB,
                                           30
                                          );
            }
        }
        
        
        static bool TestHover(ref ImDrawListPtr drawList)
        {
            var foreground = ImGui.GetForegroundDrawList();
            if (drawList._Path.Size < 2)
                return false;

            var p1 = drawList._Path[0];
            var p2 = drawList._Path[drawList._Path.Size - 1];
            //foreground.AddRect(p1, p2, Color.Orange);
            var r = new ImRect(p2, p1).MakePositive();

            r.Expand(10);
            var mousePos = ImGui.GetMousePos();
            if (!r.Contains(mousePos))
                return false;

            var pLast = p1;
            for (int i = 1; i < drawList._Path.Size; i++)
            {
                var p = drawList._Path[i];

                r = new ImRect(pLast, p).MakePositive();
                r.Expand(10);
                foreground.AddRect(r.Min, r.Max, Color.Gray);
                if (r.Contains(mousePos))
                {
                    foreground.AddRect(r.Min, r.Max, Color.Orange);
                    var v = (pLast - p);
                    var vLen = v.Length();
                    
                    var d = Vector2.Dot(v, mousePos-p) / vLen;
                    foreground.AddCircleFilled(p + v * d/vLen, 4f, Color.Red);
                    // Log.Debug("inside: " +d );
                    return true;
                }

                pLast = p;
            }

            return false;
        }
    }
}