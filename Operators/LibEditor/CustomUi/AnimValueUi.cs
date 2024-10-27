using System;
using System.Numerics;
using ImGuiNET;
using Lib.anim;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace libEditor.CustomUi;

public static class AnimValueUi
{
    public static SymbolUi.Child.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect, Vector2 canvasScale)
    {
        if (!(instance is AnimValue animValue)
            || !ImGui.IsRectVisible(screenRect.Min, screenRect.Max))
            return SymbolUi.Child.CustomUiResult.None;

        ImGui.PushID(instance.SymbolChildId.GetHashCode());
        if (WidgetElements.DrawRateLabelWithTitle(animValue.Rate, screenRect, drawList,  "Anim " + (AnimMath.Shapes)animValue.Shape.TypedInputValue.Value, canvasScale))
        {
            animValue.Rate.Input.IsDefault = false;
            animValue.Rate.DirtyFlag.Invalidate();
        }

        var h = screenRect.GetHeight();
        var graphRect = screenRect;
            
        const float relativeGraphWidth = 0.75f;
            
        graphRect.Expand(-3);
        graphRect.Min.X = graphRect.Max.X - graphRect.GetWidth() * relativeGraphWidth;
            
            
        var highlightEditable = ImGui.GetIO().KeyCtrl;

        if (h > 14 * T3Ui.UiScaleFactor)
        {
            ValueLabel.Draw(drawList, graphRect, new Vector2(1, 0), animValue.Amplitude);
            ValueLabel.Draw(drawList, graphRect, new Vector2(1, 1), animValue.Offset);
        }

        // Graph dragging to edit Bias and Ratio
        var isActive = false;

        ImGui.SetCursorScreenPos(graphRect.Min);
        if (ImGui.GetIO().KeyCtrl)
        {
            ImGui.InvisibleButton("dragMicroGraph", graphRect.GetSize());
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup) && ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsItemActive())
            {
                isActive = true;
            }

            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup))
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
                _dragStartBias = animValue.Bias.TypedInputValue.Value;
                _dragStartRatio = animValue.Ratio.TypedInputValue.Value;
            }

            if (Math.Abs(dragDelta.X) > 0.5f)
            {
                animValue.Ratio.TypedInputValue.Value = (_dragStartRatio + dragDelta.X / 100f).Clamp(0.001f, 1f);
                animValue.Ratio.DirtyFlag.Invalidate();
                animValue.Ratio.Input.IsDefault = false;
            }

            if (Math.Abs(dragDelta.Y) > 0.5f)
            {
                animValue.Bias.TypedInputValue.Value = (_dragStartBias - dragDelta.Y / 100f).Clamp(0.01f, 0.99f);
                animValue.Bias.DirtyFlag.Invalidate();
                animValue.Bias.Input.IsDefault = false;
            }
        }
            
        DrawCurve(drawList, graphRect, animValue, highlightEditable);
            
        ImGui.PopID();
        return SymbolUi.Child.CustomUiResult.Rendered 
               | SymbolUi.Child.CustomUiResult.PreventOpenSubGraph 
               | SymbolUi.Child.CustomUiResult.PreventInputLabels
               | SymbolUi.Child.CustomUiResult.PreventTooltip;

    }

    private static void DrawCurve(ImDrawListPtr drawList, ImRect graphRect, AnimValue animValue, bool highlightEditable)
    {
        var graphWidth = graphRect.GetWidth();
        var h = graphRect.GetHeight();
            
        var shapeIndex = (animValue.Shape.HasInputConnections) // Todo check for animated 
                             ? animValue.Shape.Value 
                             :animValue.Shape.TypedInputValue.Value;

        var shape = (AnimMath.Shapes)shapeIndex.Clamp(0, Enum.GetNames(typeof(AnimMath.Shapes)).Length);
            
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

            // Fragment line 
            var cycleWidth = graphWidth * (1- relativeX); 
            var dx = new Vector2((float)MathUtils.Fmod(animValue._normalizedTime,1f) * cycleWidth - 1, 0);
                
            drawList.AddRectFilled(lv1 + dx, lv2 + dx, UiColors.WidgetActiveLine);

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
                var fragment = f * (1 + previousCycleFragment) - previousCycleFragment + Math.Floor(animValue._normalizedTime);

                var v = AnimMath.CalcValueForNormalizedTime(shape,
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