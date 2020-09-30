using System;
using ImGuiNET;
using SharpDX;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.ChildUi.Animators;
using T3.Gui.Styling;
using T3.Operators.Types.Id_f0acd1a4_7a98_43ab_a807_6d1bd3e92169;
using UiHelpers;
using Vector2 = System.Numerics.Vector2;

namespace T3.Gui.ChildUi
{
    public static class RemapUi
    {
        private const float GraphRangePadding = 0.06f;
        
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect)
        {
            if (!(instance is Remap remap))
                return SymbolChildUi.CustomUiResult.None;


            screenRect.Max.X -= screenRect.GetWidth() * 0.15f; // Leave some padding at right for mode-label

            var size = screenRect.GetSize();
            
            var center = screenRect.GetCenter();
            var graphRect = screenRect;
            graphRect.Min.Y = center.Y - size.Y * 0.15f; 
            graphRect.Max.Y = center.Y + size.Y * 0.15f;

            DrawValueLabel(drawList, screenRect, new Vector2(GraphRangePadding/2, 0), remap.RangeInMin, Color.White);
            DrawValueLabel(drawList, screenRect, new Vector2(1-GraphRangePadding/2, 0), remap.RangeInMax, Color.White);
            DrawValueLabel(drawList, screenRect, new Vector2(GraphRangePadding/2, 1), remap.RangeOutMin, Color.White);
            DrawValueLabel(drawList, screenRect, new Vector2(1-GraphRangePadding/2, 1), remap.RangeOutMax, Color.White);

            // Draw interaction
            ImGui.SetCursorScreenPos(graphRect.Min);

            var value = remap.Value.Value;
            var inMin = remap.RangeInMin.TypedInputValue.Value;
            var inMax = remap.RangeInMax.TypedInputValue.Value;
            var outMin = remap.RangeOutMin.TypedInputValue.Value;
            var outMax = remap.RangeOutMax.TypedInputValue.Value;

            
            var inFragment = Math.Abs(inMin - inMax) < 0.001f ? 0 :  (value - inMin) / (inMax - inMin) ;
            var outFragment = Math.Abs(outMin - outMax) < 0.001f ? 0: (remap.Result.Value - outMin) / (outMax - outMin);

            
            // Draw graph
            //
            //       lv1       lv2
            //    0  
            //    #   |         |
            // lh 1######2------+---  [C]
            //       |   #      |
            //           3
            //            
            drawList.PushClipRect(graphRect.Min, graphRect.Max, false);

            var h = graphRect.GetHeight();
            
            // Horizontal line
            var lhMin = graphRect.Min + Vector2.UnitY * h / 2;
            var lhMax = new Vector2(graphRect.Max.X, lhMin.Y + 1);
            drawList.AddRectFilled(lhMin, lhMax, GraphLineColor);

            // Vertical start line
            var lv1Min = graphRect.Min + Vector2.UnitX * (int)(graphRect.GetWidth() * GraphRangePadding);
            var lv1Max = new Vector2(lv1Min.X + 1, graphRect.Max.Y);
            drawList.AddRectFilled(lv1Min, lv1Max, GraphLineColor);

            // Vertical end line
            var lv2Min = graphRect.Min + Vector2.UnitX * (int)(graphRect.GetWidth() * (1-GraphRangePadding));
            var lv2Max = new Vector2(lv2Min.X + 1, graphRect.Max.Y);
            drawList.AddRectFilled(lv2Min, lv2Max, GraphLineColor);

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

            drawList.AddPolyline(ref GraphLinePoints[0], 4, Color.Orange, false, 1);

            var triangleSize = TriangleSize;
            drawList.AddTriangleFilled(GraphLinePoints[3],
                                        GraphLinePoints[3] + new Vector2(-triangleSize/2 , - triangleSize ),
                                        GraphLinePoints[3] + new Vector2(+triangleSize/2 , - triangleSize ),
                                            Color.Orange
                                       );
            drawList.PopClipRect();

            return SymbolChildUi.CustomUiResult.Rendered;
        }

        private const float TriangleSize =4;
        private static bool DrawValueLabel(ImDrawListPtr drawList, ImRect screenRect, Vector2 alignment, InputSlot<float> remapValue, Color color)
        {
            var valueText = $"{remapValue.Value:G5}";

            ImGui.PushFont(Fonts.FontSmall);

            var labelSize = ImGui.CalcTextSize(valueText);
            var space = screenRect.GetSize() - labelSize;
            var position = screenRect.Min + space * alignment;

            drawList.AddText(MathUtils.Floor(position), color, valueText);
            ImGui.PopFont();

            return true;
        }
        

        private static readonly Color GraphLineColor = new Color(0, 0, 0, 0.3f);
        private static readonly Vector2[] GraphLinePoints = new Vector2[4];
    }
}