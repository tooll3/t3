using System;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.ChildUi.Animators;
using T3.Gui.Styling;
using T3.Operators.Types.Id_c5e39c67_256f_4cb9_a635_b62a0d9c796c;
using UiHelpers;

namespace T3.Gui.ChildUi
{
    public static class LfoUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect)
        {
            if (!(instance is LFO lfo)
                || !ImGui.IsRectVisible(screenRect.Min, screenRect.Max))
                return SymbolChildUi.CustomUiResult.None;

            ImGui.PushID(instance.SymbolChildId.GetHashCode());
            if (AnimatorLabel.Draw(ref lfo.Rate.TypedInputValue.Value,
                                   screenRect, drawList, nameof(lfo) + " " + (LFO.Shapes)lfo.Shape.TypedInputValue.Value))
            {
                lfo.Rate.Input.IsDefault = false;
                lfo.Rate.DirtyFlag.Invalidate();
            }

            var h = screenRect.GetHeight();
            var graphRect = screenRect;
            graphRect.Expand(-3);
            graphRect.Min.X = graphRect.Max.X - graphRect.GetWidth() * 0.5f; // GraphWidthRatio * h;

            var highlightEditable = ImGui.GetIO().KeyCtrl;

            var labelColor = highlightEditable ? Color.Blue : Color.White;
            if (h > 14)
            {
                ValueLabel.Draw(drawList, graphRect, new Vector2(1, 0), lfo.Amplitude, labelColor);
                ValueLabel.Draw(drawList, graphRect, new Vector2(1, 1), lfo.Offset, labelColor);
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
                    _dragStartBias = lfo.Bias.TypedInputValue.Value;
                    _dragStartRatio = lfo.Ratio.TypedInputValue.Value;
                }

                if (Math.Abs(dragDelta.X) > 0.5f)
                {
                    lfo.Ratio.TypedInputValue.Value = (_dragStartRatio + dragDelta.X / 100f).Clamp(0.01f, 0.99f);
                    lfo.Ratio.DirtyFlag.Invalidate();
                    lfo.Ratio.Input.IsDefault = false;
                }

                if (Math.Abs(dragDelta.Y) > 0.5f)
                {
                    lfo.Bias.TypedInputValue.Value = (_dragStartBias - dragDelta.Y / 100f).Clamp(0.01f, 0.99f);
                    lfo.Bias.DirtyFlag.Invalidate();
                    lfo.Bias.Input.IsDefault = false;
                }
            }

            // Draw Graph
            {
                // Horizontal line
                var lh1 = graphRect.Min + Vector2.UnitY * h / 2;
                var lh2 = new Vector2(graphRect.Max.X, lh1.Y + 1);
                drawList.AddRectFilled(lh1, lh2, GraphLineColor);

                // Vertical start line
                var lv1 = graphRect.Min + Vector2.UnitX * (int)(graphRect.GetWidth() * 0.1f + 0.5f);

                var lv2 = new Vector2(lv1.X + 1, graphRect.Max.Y);
                drawList.AddRectFilled(lv1, lv2, GraphLineColor);

                // Fragment line 
                var width = graphRect.GetWidth() - (lv1.X - graphRect.Min.X); //h * (GraphWidthRatio - leftPaddingH);
                var dx = new Vector2(lfo.LastFraction * width - 1, 0);
                drawList.AddRectFilled(lv1 + dx, lv2 + dx, FragmentLineColor);

                // Draw graph
                //        lv
                //        |  2-------3    y
                //        | /
                //  0-----1 - - - - - -   lh
                //        |
                //        |

                for (var i = 0; i < GraphListSteps; i++)
                {
                    var f = (float)i / GraphListSteps * 1.25f - 0.25f;
                    GraphLinePoints[i] = new Vector2(f * width,
                                                     (0.5f - lfo.CalcNormalizedValueForFraction(f) / 2) * h
                                                    ) + graphRect.Min;
                }

                var curveLineColor = highlightEditable ? Color.Red : CurveLineColor;
                drawList.AddPolyline(ref GraphLinePoints[0], GraphListSteps, curveLineColor, false, 1.5f);
            }

            ImGui.PopID();
            return SymbolChildUi.CustomUiResult.Rendered | SymbolChildUi.CustomUiResult.PreventInputLabels;
        }

        private static float _dragStartBias;
        private static float _dragStartRatio;

        private static readonly Color GraphLineColor = new Color(0, 0, 0, 0.3f);
        private static readonly Color FragmentLineColor = Color.Orange;
        private static readonly Color CurveLineColor = new Color(1, 1, 1, 0.5f);

        private static readonly Vector2[] GraphLinePoints = new Vector2[GraphListSteps];
        private const int GraphListSteps = 80;
    }
}