using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Operators.Types.Id_8211249d_7a26_4ad0_8d84_56da72a5c536;
using UiHelpers;

namespace T3.Gui.ChildUi
{
    public static class GradientSliderUi
    {
        public static bool DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect)
        {
            if (!(instance is GradientSlider gradientSlider))
                return false;

            var innerRect = selectableScreenRect;
            innerRect.Expand(-4);

            DrawEditGradient(_gradient, drawList, innerRect);
            return true;
        }

        private static readonly Vector2 StepHandleSize = new Vector2(10, 20);
        private static Gradient _gradient = new Gradient();

        public static void DrawEditGradient(Gradient gradient, ImDrawListPtr drawList, ImRect innerRect)
        {
            drawList.AddRect(innerRect.Min, innerRect.Max, Color.Black);

            //gradient.Steps.OrderBy(o => o.NormalizedPosition);
            gradient.Steps.Sort((x, y) => x.NormalizedPosition.CompareTo(y.NormalizedPosition));

            // Draw Gradient
            var lastColor = ImGui.ColorConvertFloat4ToU32(gradient.Steps[0].Color);
            var lastPos = innerRect.Min;
            var maxPos = innerRect.Max;
            foreach (var step in gradient.Steps)
            {
                var color = ImGui.ColorConvertFloat4ToU32(step.Color);
                maxPos.X = innerRect.Min.X + innerRect.GetWidth() * step.NormalizedPosition;
                drawList.AddRectFilledMultiColor(lastPos,
                                                  maxPos,
                                                  lastColor,
                                                  color,
                                                  color,
                                                  lastColor);
                lastPos.X = maxPos.X;
                lastColor = color;
            }

            if (lastPos.X < innerRect.Max.X)
            {
                drawList.AddRectFilled(lastPos, innerRect.Max, lastColor);
            } 
            
            
            // Draw handles
            foreach (var step in gradient.Steps)
            {
                ImGui.PushID(step.Id.GetHashCode());
                var x = innerRect.Min.X - StepHandleSize.X / 2f + innerRect.GetWidth() * step.NormalizedPosition;

                var stepAreaMin = new Vector2(x, innerRect.Min.Y);
                var stepAreaMax = new Vector2(x + StepHandleSize.X, innerRect.Max.Y);

                drawList.AddRectFilled(stepAreaMin, stepAreaMax, ImGui.ColorConvertFloat4ToU32(step.Color));
                drawList.AddRect(stepAreaMin, stepAreaMax, Color.Black);

                drawList.AddRect(stepAreaMin+ Vector2.One, stepAreaMax- Vector2.One, Color.White);

                ImGui.SetCursorScreenPos(stepAreaMin);
                ImGui.InvisibleButton("gradientStep", new Vector2(StepHandleSize.X, innerRect.GetHeight()));

                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0))
                {
                    step.NormalizedPosition = ((ImGui.GetMousePos().X - innerRect.Min.X) / innerRect.GetWidth()).Clamp(0, 1);

                    // valueSlider.Input.TypedInputValue.Value = newT;
                    // valueSlider.Input.Value = newT;
                    // valueSlider.Input.DirtyFlag.Invalidate();
                }

                if(ImGui.IsItemHovered() 
                   && ImGui.IsMouseReleased(0) 
                   && ImGui.GetIO().MouseDragMaxDistanceAbs[0].LengthSquared() < 2
                   && !ImGui.IsPopupOpen("##colorEdit")) 
                    ImGui.OpenPopup("##colorEdit");
                
                if (ImGui.BeginPopupContextItem("##colorEdit"))
                {
                    ImGui.ColorPicker4("edit", ref step.Color, ImGuiColorEditFlags.Float| ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreview);
                    ImGui.EndPopup();
                }

                ImGui.PopID();
            }
            
            // Insert new range
            if (innerRect.GetHeight() > MinInsertHeight)
            {
                var insertRangeMin = new Vector2(innerRect.Min.X, innerRect.Max.Y - MinInsertHeight *0.5f);
                
                drawList.AddRectFilled(insertRangeMin, innerRect.Max, new Color(0,0,0,0.1f));
                ImGui.SetCursorScreenPos(insertRangeMin);
                if (ImGui.InvisibleButton("insertRange",  innerRect.Max - insertRangeMin))
                {
                    gradient.Steps.Add(new GradientStep()
                                           {
                                               NormalizedPosition =   (ImGui.GetMousePos().X - insertRangeMin.X) / innerRect.GetWidth(),
                                               Id =  Guid.NewGuid(),
                                           });
                }
            }
        }

        private const int MinInsertHeight = 20;

        public class Gradient
        {
            public List<GradientStep> Steps = new List<GradientStep>()
                                                   {
                                                       new GradientStep()
                                                           {
                                                               NormalizedPosition = 0,
                                                               Color = new Vector4(1, 0, 1, 1),
                                                               Id = Guid.NewGuid(),
                                                           },
                                                       new GradientStep()
                                                           {
                                                               NormalizedPosition = 1,
                                                               Color = new Vector4(0, 0, 1, 1),
                                                               Id = Guid.NewGuid(),
                                                           },
                                                   };
        }

        public class GradientStep
        {
            public float NormalizedPosition;
            public Vector4 Color;
            public Interpolations Interpolation;
            public Guid Id;

            public enum Interpolations
            {
                Linear,
                Hold,
                Smooth,
            }
        }
    }
}