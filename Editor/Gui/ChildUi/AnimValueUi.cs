using System;
using System.Numerics;
using Editor.Gui.ChildUi.Animators;
using ImGuiNET;
using T3.Core;
using T3.Core.Operator;
using T3.Operators.Types.Id_c5e39c67_256f_4cb9_a635_b62a0d9c796c;
using UiHelpers;

namespace Editor.Gui.ChildUi
{
    public static class AnimValueUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect)
        {
            if (!(instance is AnimValue animValue)
                || !ImGui.IsRectVisible(screenRect.Min, screenRect.Max))
                return SymbolChildUi.CustomUiResult.None;

            ImGui.PushID(instance.SymbolChildId.GetHashCode());
            if (RateEditLabel.Draw(animValue.Rate, screenRect, drawList,  "Anim " + (AnimValue.Shapes)animValue.Shape.TypedInputValue.Value))
            {
                animValue.Rate.Input.IsDefault = false;
                animValue.Rate.DirtyFlag.Invalidate();
            }

            var h = screenRect.GetHeight();
            var graphRect = screenRect;
            
            const float RelativeGraphWidth = 0.75f;
            
            graphRect.Expand(-3);
            graphRect.Min.X = graphRect.Max.X - graphRect.GetWidth() * RelativeGraphWidth;
            var graphWidth = graphRect.GetWidth();
            
            var highlightEditable = ImGui.GetIO().KeyCtrl;

            if (h > 14)
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
                isActive = ImGui.IsItemActive();
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

            // Draw Graph
            {
                const float previousCycleFragment = 0.25f; 
                const float relativeX = previousCycleFragment / (1 + previousCycleFragment);
                
                // Horizontal line
                var lh1 = graphRect.Min + Vector2.UnitY * h / 2;
                var lh2 = new Vector2(graphRect.Max.X, lh1.Y + 1);
                drawList.AddRectFilled(lh1, lh2, T3Style.Colors.GraphAxisColor);

                // Vertical start line 
                var lv1 = graphRect.Min + Vector2.UnitX * (int)(graphWidth * relativeX);
                var lv2 = new Vector2(lv1.X + 1, graphRect.Max.Y);
                drawList.AddRectFilled(lv1, lv2, T3Style.Colors.GraphAxisColor);

                // Fragment line 
                //var width = graphRect.GetWidth() - (lv1.X - graphRect.Min.X); //h * (GraphWidthRatio - leftPaddingH);
                var cycleWidth = graphWidth * (1- relativeX); //h * (GraphWidthRatio - leftPaddingH);
                var dx = new Vector2(animValue.LastFraction * cycleWidth - 1, 0);
                drawList.AddRectFilled(lv1 + dx, lv2 + dx, T3Style.Colors.FragmentLineColor);

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
                    var fragment = f * (1 + previousCycleFragment) - previousCycleFragment;
                    GraphLinePoints[i] = new Vector2(f * graphWidth,
                                                     (0.5f - animValue.CalcNormalizedValueForFraction(fragment) / 2) * h
                                                    ) + graphRect.Min;
                }

                var curveLineColor = highlightEditable ? T3Style.Colors.GraphLineColorHover : T3Style.Colors.GraphLineColor;
                drawList.AddPolyline(ref GraphLinePoints[0], GraphListSteps, curveLineColor, ImDrawFlags.None, 1.5f);
            }

            ImGui.PopID();
            return SymbolChildUi.CustomUiResult.Rendered | SymbolChildUi.CustomUiResult.PreventInputLabels;
        }

        private static float _dragStartBias;
        private static float _dragStartRatio;
        
        

        private static readonly Vector2[] GraphLinePoints = new Vector2[GraphListSteps];
        private const int GraphListSteps = 80;
    }
}