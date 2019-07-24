using ImGuiNET;
using imHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using T3.Core.Logging;

namespace T3.Gui.Components
{
    /// <summary>
    /// A collection of custom ImGui components
    /// </summary>
    public partial class T3Gui
    {
        /// <summary>
        /// Renders a component that can change a numeric value
        /// </summary>       
        public static bool JogDial(string label, ref double delta, Vector2 size)
        {
            var hot = ImGui.Button(label + "###dummy", size);
            var io = ImGui.GetIO();
            if (ImGui.IsItemActive())
            {
                var fgdl = ImGui.GetForegroundDrawList();
                if (_pointsForJogDial.Count == 0
                    || (_pointsForJogDial[_pointsForJogDial.Count - 1] - io.MousePos).LengthSquared() > 500)
                {
                    _pointsForJogDial.Add(io.MousePos);
                }

                var center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * 0.5f;
                var averageC = center;
                for (var i = 3; i < _pointsForJogDial.Count; i++)
                {
                    var p1 = _pointsForJogDial[i];
                    var p2 = _pointsForJogDial[i - 1];
                    var p3 = _pointsForJogDial[i - 2];
                    var v12 = p2 - p1;
                    var v23 = p3 - p2;

                    var A1 = (p1 + p2) / 2;
                    var B1 = (p2 + p3) / 2;
                    var A2 = A1 + new Vector2(-v12.Y, v12.X);
                    var B2 = B1 + new Vector2(-v23.Y, v23.X);

                    var C = LineIntersection(A1, A2, B1, B2);
                    var distanceToAverage = (p1 - averageC).Length();
                    var distanceToC = (A1 - C).Length();
                    var isValid = (C - averageC).Length() < 60;
                    if (isValid)
                        averageC = Im.Lerp(averageC, C, 0.1f);

                    //fgdl.AddCircle(averageC, 3, Color.White);
                    //fgdl.AddRect(C, C, Color.Red);
                    //fgdl.AddLine(A1, C, isValid ? Color.Green : new Color(0.2f));
                    //fgdl.AddRect(p1, p1, Color.Red);
                    //fgdl.AddRect(A1, A1, Color.Gray);
                }

                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                fgdl.AddCircle(averageC, 40, new Color(0.05f), 20);
                //fgdl.AddCircle(averageC, 60, new Color(0.05f), 20);
                fgdl.AddCircle(averageC, 130, new Color(0.05f), 20);
                fgdl.AddCircle(averageC, 300, new Color(0.05f), 20);
                hot = true;

                var pLast = io.MousePos - io.MouseDelta - averageC;
                var pNow = io.MousePos - averageC;
                var aLast = Math.Atan2(pLast.X, pLast.Y);
                var aNow = Math.Atan2(pNow.X, pNow.Y);
                delta = aNow - aLast;
                if (delta > 1.5)
                {
                    delta -= 2 * Math.PI;
                }
                else if (delta < -1.5)
                {
                    delta += 2 * Math.PI;
                }

                // ignore if too close to center
                var distToC = (averageC - io.MousePos).Length();
                var smooth = Smoothstep(5, 40, distToC);
                delta *= smooth;

                var doubleSpeed = Smoothstep(120, 140, distToC) * 10 + 1;
                delta *= doubleSpeed;

                var trippleSpeed = Smoothstep(290, 310, distToC) * 20 + 1;
                delta *= trippleSpeed;

                delta *= -0.1f;
                fgdl.PathArcTo(averageC, 120, -3.1415f / 2, (-(float)_totalDeltaForDial / -3.1415f % (4 * 3.1415f) - 3.1415f) / 2, 32);
                fgdl.PathStroke(Color.White, false);
                _totalDeltaForDial += delta;
            }
            else
            {
                _pointsForJogDial.Clear();
            }
            return hot;
        }
        static List<Vector2> _pointsForJogDial = new List<Vector2>();
        static double _totalDeltaForDial = 0;


        public static float Smoothstep(float edge0, float edge1, float x)
        {
            // Scale, bias and saturate x to 0..1 range
            x = Im.Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);
            // Evaluate polynomial
            return x * x * (3 - 2 * x);
        }

        /// <remarks>
        /// http://paulbourke.net/geometry/pointlineplane/
        /// </remarks>
        private static Vector2 LineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            //function line_intersect(x1, y1, x2, y2, x3, y3, x4, y4)
            //{
            var x1 = p1.X;
            var x2 = p2.X;
            var x3 = p3.X;
            var x4 = p4.X;

            var y1 = p1.Y;
            var y2 = p2.Y;
            var y3 = p3.Y;
            var y4 = p4.Y;

            float ua, ub, denom = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);
            if (denom == 0)
            {
                return Vector2.Zero;
            }
            ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / denom;
            ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / denom;
            return new Vector2(
                x1 + ua * (x2 - x1),
                y1 + ua * (y2 - y1));
            //seg1: ua >= 0 && ua <= 1,
            //seg2: ub >= 0 && ub <= 1
            //        };
        }



        /// <summary>Draw a splitter</summary>
        /// <remarks>
        /// Take from https://github.com/ocornut/imgui/issues/319#issuecomment-147364392
        /// </remarks>
        public static void SplitFromBottom(ref float offsetFromBottom)
        {
            const float thickness = 5; ;

            var backup_pos = ImGui.GetCursorPos();

            var size = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
            var contentMin = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos();

            var pos = new Vector2(contentMin.X, contentMin.Y + size.Y - offsetFromBottom - thickness);
            ImGui.SetCursorScreenPos(pos);

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0, 0, 0, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0, 0, 0, 1));

            ImGui.Button("##Splitter", new Vector2(-1, thickness));

            ImGui.PopStyleColor(3);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
            }

            if (ImGui.IsItemActive())
            {
                offsetFromBottom = Im.Clamp(
                    offsetFromBottom - ImGui.GetIO().MouseDelta.Y,
                    0,
                    size.Y - thickness);
            }

            ImGui.SetCursorPos(backup_pos);
        }


        public static bool ToggleButton(string label, ref bool isSelected, Vector2 size, bool trigger = false)
        {
            var wasSelected = isSelected;
            var clicked = false;
            if (isSelected)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, Color.Red.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Color.Red.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, Color.Red.Rgba);
            }
            if (ImGui.Button(label, size) || trigger)
            {
                isSelected = !isSelected;
                clicked = true;
            }

            if (wasSelected)
            {
                ImGui.PopStyleColor(3);
            }
            return clicked;
        }
    }
}
