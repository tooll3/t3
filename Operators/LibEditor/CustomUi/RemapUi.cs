using ImGuiNET;
using Lib.numbers.@float.adjust;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using Vector2 = System.Numerics.Vector2;

namespace libEditor.CustomUi;

public static class RemapUi
{
    private const float GraphRangePadding = 0.06f;

    public static SymbolUi.Child.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect, Vector2 canvasScale)
    {
        if (instance is not Remap remap
            || !ImGui.IsRectVisible(screenRect.Min, screenRect.Max))
            return SymbolUi.Child.CustomUiResult.None;

        screenRect.Expand(-2);
        var biasGraphRect = screenRect;

        var isActive = false;
        
        // Draw interaction
        ImGui.SetCursorScreenPos(biasGraphRect.Min);

        var value = remap.Value.GetCurrentValue();
        var inMin = remap.RangeInMin.GetCurrentValue();
        var inMax = remap.RangeInMax.GetCurrentValue();
        var outMin = remap.RangeOutMin.GetCurrentValue();
        var outMax = remap.RangeOutMax.GetCurrentValue();

        var inFragment = Math.Abs(inMin - inMax) < 0.001f ? 0 : (value - inMin) / (inMax - inMin);
        var outFragment = Math.Abs(outMin - outMax) < 0.001f ? 0 : (remap.Result.Value - outMin) / (outMax - outMin);
        
        drawList.PushClipRect(biasGraphRect.Min, biasGraphRect.Max, true);

        var canvasFade = canvasScale.X.RemapAndClamp(0.7f, 1.5f, 0, 1);
        
        // Draw mapping graph...
        {
            const int steps = 35;
            var points = new Vector2[steps];
            var biasAndGain =  remap.BiasAndGain.GetCurrentValue();
            var p = new Vector2(MathUtils.Lerp(biasGraphRect.Min.X, biasGraphRect.Max.X, inFragment),
                                MathUtils.Lerp(biasGraphRect.Max.Y, biasGraphRect.Min.Y, outFragment));
            drawList.AddCircleFilled(p,
                                     3,
                                     UiColors.StatusAnimated);

            // Distribution...
            for (var i = 0; i < steps; i++)
            {
                var t = (float)i / (steps - 1);
                var f = t.ApplyBiasAndGain(biasAndGain.X, biasAndGain.Y);
                var x = MathUtils.Lerp(biasGraphRect.Min.X, biasGraphRect.Max.X, f);
                var y = MathUtils.Lerp(biasGraphRect.Max.Y, biasGraphRect.Min.Y, f);
                drawList.AddLine(new Vector2(biasGraphRect.Min.X, y), new
                                     Vector2(biasGraphRect.Max.X, y),
                                 UiColors.BackgroundFull.Fade(0.2f * canvasFade), 1);

                points[i] = new Vector2(MathUtils.Lerp(biasGraphRect.Min.X, biasGraphRect.Max.X, t),
                                        MathUtils.Lerp(biasGraphRect.Min.Y, biasGraphRect.Max.Y, 1 - f));
            }

            drawList.AddLine(new Vector2(p.X, biasGraphRect.Min.Y),
                             new Vector2(p.X, biasGraphRect.Max.Y), UiColors.StatusAnimated.Fade(0.5f), 0.5f);
                
            drawList.AddLine(p, new Vector2(biasGraphRect.Max.X -5 , p.Y), UiColors.StatusAnimated.Fade(0.5f), 1);
            drawList.AddRectFilled(new Vector2(biasGraphRect.Max.X -3 , p.Y),
                                   biasGraphRect.Max, UiColors.StatusAnimated);

            drawList.AddPolyline(ref points[0], steps, UiColors.TextMuted, ImDrawFlags.None, 1);
            drawList.PopClipRect();
        }
            
        if (inFragment > 1)
        {
            drawList.AddCircleFilled(new Vector2( biasGraphRect.Max.X-4, biasGraphRect.Max.Y-4), 4, UiColors.StatusAnimated, 3);
        }
            
        if (inFragment < 0)
        {
            drawList.AddTriangleFilled(new Vector2( biasGraphRect.Min.X+7, biasGraphRect.Max.Y-9.5f),
                                       new Vector2( biasGraphRect.Min.X+2, biasGraphRect.Max.Y-6.5f),
                                       new Vector2( biasGraphRect.Min.X+7, biasGraphRect.Max.Y-3.5f),
                                       UiColors.StatusAnimated
                                      );
        }
            
        isActive |= ValueLabel.Draw(drawList, screenRect, new Vector2(GraphRangePadding / 2, 0), remap.RangeInMax);
        isActive |= ValueLabel.Draw(drawList, screenRect, new Vector2(GraphRangePadding / 2, 1), remap.RangeInMin);

        isActive |= ValueLabel.Draw(drawList, screenRect, new Vector2(1 - GraphRangePadding / 2, 0), remap.RangeOutMax);
        isActive |= ValueLabel.Draw(drawList, screenRect, new Vector2(1 - GraphRangePadding / 2, 1), remap.RangeOutMin);

        return SymbolUi.Child.CustomUiResult.Rendered 
               | SymbolUi.Child.CustomUiResult.PreventInputLabels 
               | SymbolUi.Child.CustomUiResult.PreventOpenSubGraph 
               | SymbolUi.Child.CustomUiResult.PreventTooltip
               | (isActive ? SymbolUi.Child.CustomUiResult.IsActive : SymbolUi.Child.CustomUiResult.None);
    }
}