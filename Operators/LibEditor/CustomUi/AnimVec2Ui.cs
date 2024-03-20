using System;
using System.Numerics;
using ImGuiNET;
using lib.anim;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace libEditor.CustomUi
{
    public static class AnimVec2Ui
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect, Vector2 canvasScale)
        {
            if (!(instance is AnimVec2 animVec2)
                || !ImGui.IsRectVisible(screenRect.Min, screenRect.Max))
                return SymbolChildUi.CustomUiResult.None;

            ImGui.PushID(instance.SymbolChildId.GetHashCode());
            if (WidgetElements.DrawRateLabelWithTitle(animVec2.RateFactor, screenRect, drawList,  "Anim2 " + (AnimMath.Shapes)animVec2.Shape.TypedInputValue.Value, canvasScale))
            {
                animVec2.RateFactor.Input.IsDefault = false;
                animVec2.RateFactor.DirtyFlag.Invalidate();
            }

            var h = screenRect.GetHeight();
            var graphRect = screenRect;
            
            const float relativeGraphWidth = 0.75f;
            
            graphRect.Expand(-3);
            graphRect.Min.X = graphRect.Max.X - graphRect.GetWidth() * relativeGraphWidth;
            
            
            var highlightEditable = ImGui.GetIO().KeyCtrl;

            if (h > 14)
            {
                ValueLabel.Draw(drawList, graphRect, new Vector2(1, 0), animVec2.AmplitudeFactor);
                //ValueLabel.Draw(drawList, graphRect, new Vector2(1, 1), animValue.Offset);
            }

            // Graph dragging to edit Bias and Ratio
            var isActive = false;

            ImGui.SetCursorScreenPos(graphRect.Min);
            if (ImGui.GetIO().KeyCtrl)
            {
                ImGui.InvisibleButton("dragMicroGraph", graphRect.GetSize());
                isActive = ImGui.IsItemActive();

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
                }
            }

            if (isActive)
            {
                var dragDelta = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left, 1);

                if (ImGui.IsItemActivated())
                {
                    //_dragStartPosition = ImGui.GetMousePos();
                    _dragStartBias = animVec2.Bias.TypedInputValue.Value;
                    _dragStartRatio = animVec2.Ratio.TypedInputValue.Value;
                }

                if (Math.Abs(dragDelta.X) > 0.5f)
                {
                    animVec2.Ratio.TypedInputValue.Value = (_dragStartRatio + dragDelta.X / 100f).Clamp(0.001f, 1f);
                    animVec2.Ratio.DirtyFlag.Invalidate();
                    animVec2.Ratio.Input.IsDefault = false;
                }

                if (Math.Abs(dragDelta.Y) > 0.5f)
                {
                    animVec2.Bias.TypedInputValue.Value = (_dragStartBias - dragDelta.Y / 100f).Clamp(0.01f, 0.99f);
                    animVec2.Bias.DirtyFlag.Invalidate();
                    animVec2.Bias.Input.IsDefault = false;
                }
            }
            
            DrawCurve(drawList, graphRect, animVec2, highlightEditable);
            
            ImGui.PopID();
            return SymbolChildUi.CustomUiResult.Rendered 
                   | SymbolChildUi.CustomUiResult.PreventOpenSubGraph 
                   | SymbolChildUi.CustomUiResult.PreventInputLabels
                   | SymbolChildUi.CustomUiResult.PreventTooltip;
        }

        private static void DrawCurve(ImDrawListPtr drawList, ImRect graphRect, AnimVec2 animValue, bool highlightEditable)
        {
            var graphWidth = graphRect.GetWidth();
            var h = graphRect.GetHeight();
            
            // Draw Graph
            {
                const float previousCycleFragment = 0.25f; 
                const float relativeX = previousCycleFragment / (1 + previousCycleFragment);
                
                // Horizontal line
                var lh1 = graphRect.Min + Vector2.UnitY * h / 2;
                var lh2 = new Vector2(graphRect.Max.X, lh1.Y + 1);
                drawList.AddRectFilled(lh1, lh2, UiColors.WidgetAxis);

                // Vertical start line 
                var lv1 = graphRect.Min + Vector2.UnitX * (int)(graphWidth * relativeX);
                var lv2 = new Vector2(lv1.X + 1, graphRect.Max.Y);
                drawList.AddRectFilled(lv1, lv2, UiColors.WidgetAxis);

                // Fragment lines 
                {
                    var cycleWidth = graphWidth * (1- relativeX); 
                    var dx = new Vector2((float)MathUtils.Fmod(animValue._normalizedTimeX,1f) * cycleWidth - 1, 0);
                    drawList.AddRectFilled(lv1 + dx, lv2 + dx, UiColors.WidgetActiveLine);
                }

                {
                    var cycleWidth = graphWidth * (1- relativeX); 
                    var dx = new Vector2((float)MathUtils.Fmod(animValue._normalizedTimeY,1f) * cycleWidth - 1, 0);
                    drawList.AddRectFilled(lv1 + dx, lv2 + dx, UiColors.WidgetActiveLine);
                }


                
                // Draw graph
                //        lv
                //        |  2-------3    y
                //        | /
                //  0-----1 - - - - - -   lh
                //        |
                //        |
                
                for (var i = 0; i < GraphListSteps; i++)
                {
                    var f = (float)i / GraphListSteps;
                    var fragment = f * (1 + previousCycleFragment) - previousCycleFragment + Math.Floor(animValue._normalizedTimeX);

                    var v = AnimMath.CalcValueForNormalizedTime(animValue._shape,
                                                                fragment,
                                                                0,
                                                                animValue.Bias.TypedInputValue.Value,
                                                                animValue.Ratio.TypedInputValue.Value).Clamp(-1,1); 
                    var vv = (0.5f - v / 2) * h;

                    _graphLinePoints[i] = new Vector2(f * graphWidth,
                                                      vv
                                                     ) + graphRect.Min;
                }

                var curveLineColor = highlightEditable ? UiColors.WidgetLineHover : UiColors.WidgetLine;
                drawList.AddPolyline(ref _graphLinePoints[0], GraphListSteps, curveLineColor, ImDrawFlags.None, 1.5f);
            }            
        }
        

        private static float _dragStartBias;
        private static float _dragStartRatio;
        
        private static readonly Vector2[] _graphLinePoints = new Vector2[GraphListSteps];
        private const int GraphListSteps = 80;
    }
}