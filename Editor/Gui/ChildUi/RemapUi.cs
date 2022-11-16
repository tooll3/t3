using System;
using Editor.Gui;
using Editor.Gui.ChildUi;
using ImGuiNET;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Core.Utils;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Operators.Types.Id_f0acd1a4_7a98_43ab_a807_6d1bd3e92169;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.ChildUi
{
    public static class RemapUi
    {
        private const float GraphRangePadding = 0.06f;

        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect)
        {
            if (!(instance is Remap remap)
                || !ImGui.IsRectVisible(screenRect.Min, screenRect.Max))
                return SymbolChildUi.CustomUiResult.None;

            screenRect.Max.X -= screenRect.GetWidth() * 0.15f; // Leave some padding at right for mode-label

            var size = screenRect.GetSize();

            var center = screenRect.GetCenter();
            var graphRect = screenRect;
            graphRect.Min.Y = center.Y - size.Y * 0.15f;
            graphRect.Max.Y = center.Y + size.Y * 0.15f;

            ValueLabel.Draw(drawList, screenRect, new Vector2(GraphRangePadding / 2, 0), remap.RangeInMin);
            ValueLabel.Draw(drawList, screenRect, new Vector2(1 - GraphRangePadding / 2, 0), remap.RangeInMax);
            ValueLabel.Draw(drawList, screenRect, new Vector2(GraphRangePadding / 2, 1), remap.RangeOutMin);
            ValueLabel.Draw(drawList, screenRect, new Vector2(1 - GraphRangePadding / 2, 1), remap.RangeOutMax);

            // Draw interaction
            ImGui.SetCursorScreenPos(graphRect.Min);

            var value = remap.Value.Value;
            var inMin = remap.RangeInMin.TypedInputValue.Value;
            var inMax = remap.RangeInMax.TypedInputValue.Value;
            var outMin = remap.RangeOutMin.TypedInputValue.Value;
            var outMax = remap.RangeOutMax.TypedInputValue.Value;

            var inFragment = Math.Abs(inMin - inMax) < 0.001f ? 0 : (value - inMin) / (inMax - inMin);
            var outFragment = Math.Abs(outMin - outMax) < 0.001f ? 0 : (remap.Result.Value - outMin) / (outMax - outMin);

            // Draw graph
            //
            //       lv1       lv2
            //    0  
            //    #   |         |
            // lh 1######2------+---  [C]
            //       |   #      |
            //           3
            //            
            drawList.PushClipRect(graphRect.Min, graphRect.Max, true);

            var h = graphRect.GetHeight();

            // Horizontal line
            var lhMin = graphRect.Min + Vector2.UnitY * h / 2;
            var lhMax = new Vector2(graphRect.Max.X, lhMin.Y + 1);
            drawList.AddRectFilled(lhMin, lhMax, T3Style.Colors.GraphAxisColor);

            // Vertical start line
            var lv1Min = graphRect.Min + Vector2.UnitX * (int)(graphRect.GetWidth() * GraphRangePadding);
            var lv1Max = new Vector2(lv1Min.X + 1, graphRect.Max.Y);
            drawList.AddRectFilled(lv1Min, lv1Max, T3Style.Colors.GraphAxisColor);

            // Vertical end line
            var lv2Min = graphRect.Min + Vector2.UnitX * (int)(graphRect.GetWidth() * (1 - GraphRangePadding));
            var lv2Max = new Vector2(lv2Min.X + 1, graphRect.Max.Y);
            drawList.AddRectFilled(lv2Min, lv2Max, T3Style.Colors.GraphAxisColor);

            var inputX = MathUtils.Lerp(lv1Min.X, lv2Min.X, inFragment);
            GraphLinePoints[0].X = inputX;
            GraphLinePoints[0].Y = graphRect.Min.Y;

            GraphLinePoints[1].X = inputX;
            GraphLinePoints[1].Y = graphRect.GetCenter().Y;

            var outputX = MathUtils.Lerp(lv1Min.X, lv2Min.X, outFragment);
            GraphLinePoints[2].X = outputX;
            GraphLinePoints[2].Y = graphRect.GetCenter().Y;

            GraphLinePoints[3].X = outputX;
            GraphLinePoints[3].Y = graphRect.Max.Y;

            drawList.AddPolyline(ref GraphLinePoints[0], 4, Color.Orange, ImDrawFlags.None, 1);

            var triangleSize = TriangleSize;
            drawList.AddTriangleFilled(GraphLinePoints[3],
                                       GraphLinePoints[3] + new Vector2(-triangleSize / 2, -triangleSize),
                                       GraphLinePoints[3] + new Vector2(+triangleSize / 2, -triangleSize),
                                       Color.Orange
                                      );
            drawList.PopClipRect();

            return SymbolChildUi.CustomUiResult.Rendered | SymbolChildUi.CustomUiResult.PreventInputLabels | SymbolChildUi.CustomUiResult.PreventOpenSubGraph;
        }

        private const float TriangleSize = 4;
        
        private static readonly Vector2[] GraphLinePoints = new Vector2[4];
    }
}