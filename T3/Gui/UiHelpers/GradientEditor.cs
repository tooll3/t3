using System;
using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes;
using UiHelpers;

namespace T3.Gui.UiHelpers
{
    public static class GradientEditor
    {

        /// <summary>
        /// Draw a gradient control that returns true, if gradient has been modified
        /// </summary>
        public static bool Draw(Gradient gradient, ImDrawListPtr drawList, ImRect areaOnScreen)
        {
            var modified = false;
            drawList.AddRect(areaOnScreen.Min, areaOnScreen.Max, Color.Black);

            //gradient.Steps.OrderBy(o => o.NormalizedPosition);
            gradient.Steps.Sort((x, y) => x.NormalizedPosition.CompareTo(y.NormalizedPosition));

            // Draw Gradient
            var lastColor = ImGui.ColorConvertFloat4ToU32(gradient.Steps[0].Color);
            var lastPos = areaOnScreen.Min;
            var maxPos = areaOnScreen.Max;
            foreach (var step in gradient.Steps)
            {
                var color = ImGui.ColorConvertFloat4ToU32(step.Color);
                maxPos.X = areaOnScreen.Min.X + areaOnScreen.GetWidth() * step.NormalizedPosition;
                drawList.AddRectFilledMultiColor(lastPos,
                                                  maxPos,
                                                  lastColor,
                                                  color,
                                                  color,
                                                  lastColor);
                lastPos.X = maxPos.X;
                lastColor = color;
            }

            if (lastPos.X < areaOnScreen.Max.X)
            {
                drawList.AddRectFilled(lastPos, areaOnScreen.Max, lastColor);
            } 
            
            
            // Draw handles
            foreach (var step in gradient.Steps)
            {
                ImGui.PushID(step.Id.GetHashCode());
                var x = areaOnScreen.Min.X - StepHandleSize.X / 2f + areaOnScreen.GetWidth() * step.NormalizedPosition;

                var stepAreaMin = new Vector2(x, areaOnScreen.Min.Y);
                var stepAreaMax = new Vector2(x + StepHandleSize.X, areaOnScreen.Max.Y);

                drawList.AddRectFilled(stepAreaMin, stepAreaMax, ImGui.ColorConvertFloat4ToU32(step.Color));
                drawList.AddRect(stepAreaMin, stepAreaMax, Color.Black);

                drawList.AddRect(stepAreaMin+ Vector2.One, stepAreaMax- Vector2.One, Color.White);

                ImGui.SetCursorScreenPos(stepAreaMin);
                ImGui.InvisibleButton("gradientStep", new Vector2(StepHandleSize.X, areaOnScreen.GetHeight()));

                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0))
                {
                    step.NormalizedPosition = ((ImGui.GetMousePos().X - areaOnScreen.Min.X) / areaOnScreen.GetWidth()).Clamp(0, 1);
                    modified = true;
                }

                if(ImGui.IsItemHovered() 
                   && ImGui.IsMouseReleased(0) 
                   && ImGui.GetIO().MouseDragMaxDistanceAbs[0].LengthSquared() < 2
                   && !ImGui.IsPopupOpen("##colorEdit")) 
                    ImGui.OpenPopup("##colorEdit");
                
                if (ImGui.BeginPopupContextItem("##colorEdit"))
                {
                    modified= ImGui.ColorPicker4("edit", ref step.Color, ImGuiColorEditFlags.Float| ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreview);
                    ImGui.EndPopup();
                }

                ImGui.PopID();
            }
            
            // Insert new range
            if (areaOnScreen.GetHeight() > MinInsertHeight)
            {
                var insertRangeMin = new Vector2(areaOnScreen.Min.X, areaOnScreen.Max.Y - MinInsertHeight *0.5f);
                
                drawList.AddRectFilled(insertRangeMin, areaOnScreen.Max, new Color(0,0,0,0.1f));
                ImGui.SetCursorScreenPos(insertRangeMin);
                if (ImGui.InvisibleButton("insertRange",  areaOnScreen.Max - insertRangeMin))
                {
                    gradient.Steps.Add(new Gradient.Step()
                                           {
                                               NormalizedPosition =   (ImGui.GetMousePos().X - insertRangeMin.X) / areaOnScreen.GetWidth(),
                                               Id =  Guid.NewGuid(),
                                           });
                    modified = true;
                }
            }

            return modified;
        }

        private const int MinInsertHeight = 20;
        private static readonly Vector2 StepHandleSize = new Vector2(10, 20);
    }
}