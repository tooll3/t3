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
            
            DrawGraph(lfo,screenRect, drawList);

            ImGui.PopID();
            return SymbolChildUi.CustomUiResult.Rendered |  SymbolChildUi.CustomUiResult.PreventInputLabels;
        }
        
        
        public static void DrawGraph(LFO lfo, ImRect innerRect, ImDrawListPtr drawList)
        {
            var inc = lfo.Amplitude.Value;
            var label = (inc < 0 ? "-" : "+") + $"{inc:0.0}";
            if (Math.Abs(lfo.Offset.Value) > 0.01f)
            {
                label += $" % {lfo.Offset.Value:0}";
            }            
            
            var h = innerRect.GetHeight();
            var graphRect = innerRect;
            graphRect.Min.X = graphRect.Max.X - graphRect.GetWidth() * 0.5f; // GraphWidthRatio * h;

            var highlightEditable = ImGui.GetIO().KeyCtrl;

            // Draw interaction
            ImGui.SetCursorScreenPos(graphRect.Min);
            var isActive = false;

            if (ImGui.GetIO().KeyCtrl)
            {
                ImGui.InvisibleButton("dragMicroGraph", graphRect.GetSize());

                // if (ImGui.IsItemHovered())
                // {
                //     ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
                // }
                isActive = ImGui.IsItemActive();
            }
            
            if (isActive)
            {
                var dragDelta = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left, 1);
                // switch (_dragState)
                // {
                //     case DragMode.Off:
                //     case DragMode.Undecided:
                //     {
                //         if (dragDelta.LengthSquared() > 10)
                //         {
                //             _dragState = Math.Abs(dragDelta.X) > Math.Abs(dragDelta.Y)
                //                              ? DragMode.DraggingHorizontally
                //                              : DragMode.DraggingVertically;
                //             _dragStartPosition = ImGui.GetMousePos();
                //             _dragStartSmoothing = smoothing;
                //             _dragStartOffset = offset;
                //         }
                //
                //         break;
                //     }
                //
                //     case DragMode.DraggingHorizontally:
                //         if (Math.Abs(dragDelta.X) > 0.5f)
                //         {
                //             smoothing = (_dragStartSmoothing - (_dragStartPosition.X - ImGui.GetMousePos().X) / 100).Clamp(0, 1);
                //             modified = true;
                //         }
                //
                //         break;
                //
                //     case DragMode.DraggingVertically:
                //         if (Math.Abs(dragDelta.Y) > 0.5f)
                //         {
                //             var logScale = ((float)Math.Pow(1.02f, Math.Abs(dragDelta.Y)) - 1) / 100;
                //             offset = _dragStartOffset + (dragDelta.Y < 0 ? logScale : -logScale);
                //             modified = true;
                //         }
                //
                //         break;
                // }
            }
            else if (ImGui.IsItemDeactivated())
            {
                // Log.Debug("Deactivated");
                // _dragState = DragMode.Off;
            }

            // horizontal line
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
            // GraphLinePoints[0] = lh1;
            // GraphLinePoints[1].X = lv1.X;
            // GraphLinePoints[1].Y = lh1.Y;
            //
            // const float yLineRatioH = 0.25f;
            // var y = graphRect.Min.Y + yLineRatioH * h;
            //
            // GraphLinePoints[2].X = lv1.X + bias.Clamp(0, 1) * width;
            // GraphLinePoints[2].Y = y;
            //
            // GraphLinePoints[3].X = graphRect.Max.X + 1;
            // GraphLinePoints[3].Y = y;
            
            for (int i = 0; i < GraphListSteps; i++)
            {
                float f = (float)i / GraphListSteps*1.5f - 0.25f ;
                GraphLinePoints[i] = new Vector2(f * width ,
                                                 (1 - lfo.CalcNormalizedValueForFraction( MathUtils.Fmod(f,1) , (LFO.Shapes)lfo.Shape.TypedInputValue.Value, lfo.Bias.TypedInputValue.Value)) * h/2.5f
                                                 ) + graphRect.Min;
            }

            var curveLineColor =highlightEditable ? Color.Red : CurveLineColor;
            drawList.AddPolyline(ref GraphLinePoints[0], GraphListSteps, curveLineColor, false, 1);
            
            // Draw offset label
            if (h > 14)
            {
                ImGui.PushFont(Fonts.FontSmall);

                var labelSize = ImGui.CalcTextSize(label);

                var color = highlightEditable ? Color.Red : Color.White;
                drawList.AddText(MathUtils.Floor(new Vector2(graphRect.Max.X - 3 - labelSize.X,
                                                             lh1.Y - labelSize.Y / 2 - 2
                                                            )), color, label);
                ImGui.PopFont();
            }
        }

        
        
        private static Vector2 _dragStartPosition;
        // private static DragMode _dragState;
        private static float _dragStartSmoothing;
        private static float _dragStartOffset;

        private static readonly Color GraphLineColor = new Color(0, 0, 0, 0.3f);
        private static readonly Color FragmentLineColor = Color.Orange;
        private static readonly Color CurveLineColor = new Color(1, 1, 1, 0.5f);
        private const float JumpDistanceDragScale = 1.05f;

        private static readonly Vector2[] GraphLinePoints = new Vector2[GraphListSteps];
        private const int GraphListSteps = 80;

    }
}