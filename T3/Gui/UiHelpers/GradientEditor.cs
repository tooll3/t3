using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Gui.InputUi;
using T3.Gui.Interaction;
using T3.Gui.Styling;
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

            gradient.Steps.Sort((x, y) => x.NormalizedPosition.CompareTo(y.NormalizedPosition));

            DrawGradient(gradient, drawList, areaOnScreen);

            // Draw handles
            var anyHandleHovered = false;
            if (areaOnScreen.GetHeight() >= RequiredHeightForHandles)
            {
                Gradient.Step removedStep = null;
                foreach (var step in gradient.Steps)
                {
                    ImGui.PushID(step.Id.GetHashCode());
                    var handleArea = GetHandleAreaForPosition(step.NormalizedPosition);

                    // Interaction
                    ImGui.SetCursorScreenPos(handleArea.Min);
                    ImGui.InvisibleButton("gradientStep", new Vector2(StepHandleSize.X, areaOnScreen.GetHeight()));

                    // Stub for ColorEditButton that allows quick sliders. Sadly this doesn't work with right mouse button drag.
                    //modified |= ColorEditButton.Draw(ref step.Color, new Vector2(StepHandleSize.X, areaOnScreen.GetHeight()));

                    if (ImGui.IsItemHovered())
                        anyHandleHovered = true;

                    var isDraggedOutside = false;
                    if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                    {
                        if (ImGui.GetIO().KeyCtrl)
                        {
                            var previousColor = step.Color;
                            ColorEditButton.VerticalColorSlider(step.Color, handleArea.GetCenter(), step.Color.W);
                            var mouseDragDelta = ImGui.GetMouseDragDelta().Y / 100;
                            ImGui.ResetMouseDragDelta();
                            Log.Debug("drag delta = " + mouseDragDelta);
                            step.Color.W = (previousColor.W - mouseDragDelta).Clamp(0, 1);
                        }
                        else
                        {
                            step.NormalizedPosition = ((ImGui.GetMousePos().X - areaOnScreen.Min.X) / areaOnScreen.GetWidth()).Clamp(0, 1);
                        }

                        isDraggedOutside = ImGui.GetMousePos().Y > areaOnScreen.Max.Y + RemoveThreshold;
                        modified = true;
                    }

                    // Draw handle
                    if (isDraggedOutside)
                    {
                        handleArea.Min.Y += 10;
                        handleArea.Max.Y += 10;
                    }

                    if (ImGui.IsItemDeactivated())
                    {
                        var mouseOutsideThresholdAfterDrag = ImGui.GetMousePos().Y > areaOnScreen.Max.Y + RemoveThreshold;
                        if (mouseOutsideThresholdAfterDrag && gradient.Steps.Count > 1)
                            removedStep = step;
                    }

                    var points = new[]
                                     {
                                         new Vector2(handleArea.Min.X, handleArea.Max.Y),
                                         handleArea.Max,
                                         new Vector2(handleArea.Max.X, handleArea.Min.Y),
                                     };
                    drawList.AddConvexPolyFilled(ref points[0], 3, new Color(0.15f, 0.15f, 0.15f, 1));
                    drawList.AddRectFilled(handleArea.Min, handleArea.Max, ImGui.ColorConvertFloat4ToU32(step.Color));
                    drawList.AddRect(handleArea.Min, handleArea.Max, Color.Black);
                    drawList.AddRect(handleArea.Min + Vector2.One, handleArea.Max - Vector2.One, Color.White);

                    if (ImGui.IsItemHovered()
                        && ImGui.IsMouseReleased(0)
                        && ImGui.GetIO().MouseDragMaxDistanceAbs[0].Length() < UserSettings.Config.ClickThreshold
                        && !ImGui.IsPopupOpen("##colorEdit"))
                    {
                        T3Ui.OpenedPopUpName = "##colorEdit";
                        ImGui.OpenPopup("##colorEdit");
                        ImGui.SetNextWindowPos(new Vector2(handleArea.Min.X, handleArea.Max.Y));
                    }

                    //anyHandleHovered = true;
                    var popUpResult = ColorEditPopup.DrawPopup(ref step.Color, step.Color);
                    modified |= popUpResult == InputEditStateFlags.Nothing;
                    ImGui.PopID();
                }

                if (removedStep != null)
                    gradient.Steps.Remove(removedStep);

                // Insert new range
                if (areaOnScreen.GetHeight() > MinInsertHeight)
                {
                    var insertRangeMin = new Vector2(areaOnScreen.Min.X, areaOnScreen.Max.Y - StepHandleSize.Y);
                    ImGui.SetCursorScreenPos(insertRangeMin);

                    var normalizedPosition = (ImGui.GetMousePos().X - insertRangeMin.X) / areaOnScreen.GetWidth();

                    if (ImGui.InvisibleButton("insertRange", areaOnScreen.Max - insertRangeMin))
                    {
                        gradient.Steps.Add(new Gradient.Step()
                                               {
                                                   NormalizedPosition = normalizedPosition,
                                                   Id = Guid.NewGuid(),
                                                   Color = gradient.Sample(normalizedPosition)
                                               });
                        modified = true;
                    }

                    if (ImGui.IsItemHovered() && !ImGui.IsItemActive() && !anyHandleHovered)
                    {
                        var handleArea = GetHandleAreaForPosition(normalizedPosition);
                        drawList.AddRect(handleArea.Min + Vector2.One, handleArea.Max - Vector2.One, new Color(1f, 1f, 1f, 0.4f));
                    }

                    CustomComponents.ContextMenuForItem(() =>
                                                        {
                                                            ImGui.Separator();

                                                            if (ImGui.BeginMenu("Gradient presets..."))
                                                            {
                                                                var foregroundDrawList = ImGui.GetForegroundDrawList();

                                                                for (var index = 0; index < UserSettings.Config.GradientPresets.Count; index++)
                                                                {
                                                                    ImGui.PushID(index);
                                                                    var preset = UserSettings.Config.GradientPresets[index];

                                                                    if (ImGui.InvisibleButton("" + index, new Vector2(100, ImGui.GetFrameHeight())))
                                                                    {
                                                                        var clone = preset.TypedClone();
                                                                        gradient.Steps = clone.Steps;
                                                                        gradient.Interpolation = clone.Interpolation;
                                                                    }

                                                                    var rect = new ImRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
                                                                    DrawGradient(preset, foregroundDrawList, rect);

                                                                    ImGui.SameLine();
                                                                    if (CustomComponents.IconButton(Icon.Trash, "##delete",
                                                                                                    Vector2.One * ImGui.GetFrameHeight()))
                                                                    {
                                                                        UserSettings.Config.GradientPresets.Remove(preset);
                                                                        UserSettings.Save();
                                                                        ImGui.PopID();
                                                                        break;
                                                                    }

                                                                    ImGui.PopID();
                                                                }

                                                                if (ImGui.MenuItem("Save"))
                                                                {
                                                                    UserSettings.Config.GradientPresets.Add(gradient.TypedClone());
                                                                    UserSettings.Save();
                                                                }

                                                                ImGui.EndMenu();
                                                            }

                                                            if (ImGui.BeginMenu("Interpolation..."))
                                                            {
                                                                foreach (Gradient.Interpolations value in Enum.GetValues(typeof(Gradient.Interpolations)))
                                                                {
                                                                    var isSelected = gradient.Interpolation == value;
                                                                    if (ImGui.MenuItem(value.ToString(), "", isSelected))
                                                                    {
                                                                        gradient.Interpolation = value;
                                                                    }
                                                                }

                                                                ImGui.EndMenu();
                                                            }
                                                        });
                }
            }

            return modified;

            ImRect GetHandleAreaForPosition(float normalizedPosition)
            {
                var x = areaOnScreen.Min.X - StepHandleSize.X / 2f + areaOnScreen.GetWidth() * normalizedPosition;
                return new ImRect(new Vector2(x, areaOnScreen.Max.Y - StepHandleSize.Y), new Vector2(x + StepHandleSize.X, areaOnScreen.Max.Y + 2));
            }
        }

        private static void DrawGradient(Gradient gradient, ImDrawListPtr drawList, ImRect areaOnScreen)
        {
            drawList.AddRect(areaOnScreen.Min, areaOnScreen.Max, Color.Black);
            drawList.AddRectFilled(areaOnScreen.Min, areaOnScreen.Max, new Color(0.15f, 0.15f, 0.15f, 1));

            if (gradient.Steps == null || gradient.Steps.Count == 0)
            {
                Log.Warning("Can't draw invalid gradient");
                return;
            }

            // Draw Gradient background
            {
                CustomComponents.FillWithStripes(drawList, areaOnScreen);
            }

            // Draw Gradient
            var minPos = areaOnScreen.Min;
            var maxPos = areaOnScreen.Max;
            
            uint leftColor = ImGui.ColorConvertFloat4ToU32(gradient.Steps[0].Color);

            // Draw complex gradient
            if (gradient.Interpolation == Gradient.Interpolations.Smooth || gradient.Interpolation == Gradient.Interpolations.OkLab)
            {
                var f = 0f;

                for (var stepIndex = 0; stepIndex < gradient.Steps.Count; stepIndex++)
                {
                    var step = gradient.Steps[stepIndex];

                    var steps = 5;

                    var rightF = step.NormalizedPosition;
                    var rangeF = (rightF - f);
                    var stepSizeF = rangeF / steps;

                    var pixelStepSize = (areaOnScreen.GetWidth() * rangeF) / steps;
                    maxPos.X = minPos.X + pixelStepSize;

                    for (int i = 0; i < steps; i++)
                    {
                        var nextF = f + stepSizeF;
                        var nextColor = ImGui.ColorConvertFloat4ToU32(gradient.Sample(nextF));

                        drawList.AddRectFilledMultiColor(minPos,
                                                         maxPos,
                                                         leftColor,
                                                         nextColor,
                                                         nextColor,
                                                         leftColor);
                        maxPos.X += pixelStepSize;
                        minPos.X += pixelStepSize;

                        f = nextF;
                        leftColor = nextColor;
                    }
                }
            }
            // Linear gradient
            else
            {
                foreach (var step in gradient.Steps)
                {
                    uint rightColor = ImGui.ColorConvertFloat4ToU32(step.Color);
                    maxPos.X = areaOnScreen.Min.X + areaOnScreen.GetWidth() * step.NormalizedPosition;
                    if (gradient.Interpolation == Gradient.Interpolations.Hold)
                    {
                        drawList.AddRectFilledMultiColor(minPos,
                                                         maxPos,
                                                         leftColor,
                                                         leftColor,
                                                         leftColor,
                                                         leftColor);
                    }
                    else
                    {
                        drawList.AddRectFilledMultiColor(minPos,
                                                         maxPos,
                                                         leftColor,
                                                         rightColor,
                                                         rightColor,
                                                         leftColor);
                    }

                    minPos.X = maxPos.X;
                    leftColor = rightColor;
                }
            }

            if (minPos.X < areaOnScreen.Max.X)
            {
                drawList.AddRectFilled(minPos, areaOnScreen.Max, leftColor);
            }
        }

        private static List<Gradient> _definedGradients = new List<Gradient>();

        private const float RemoveThreshold = 150;
        private const float RequiredHeightForHandles = 20;
        private const int MinInsertHeight = 20;
        public static readonly Vector2 StepHandleSize = new Vector2(14, 24);
    }
}