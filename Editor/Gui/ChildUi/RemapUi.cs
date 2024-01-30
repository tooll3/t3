using System;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using T3.Operators.Types.Id_f0acd1a4_7a98_43ab_a807_6d1bd3e92169;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.ChildUi
{
    public static class RemapUi
    {
        private const float GraphRangePadding = 0.06f;

        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect)
        {
            if (instance is not Remap remap
                || !ImGui.IsRectVisible(screenRect.Min, screenRect.Max))
                return SymbolChildUi.CustomUiResult.None;

            screenRect.Expand(-2);
            var biasGraphRect = screenRect;

            // Draw interaction
            ImGui.SetCursorScreenPos(biasGraphRect.Min);

            var value = remap.Value.Value;
            var inMin = remap.RangeInMin.TypedInputValue.Value;
            var inMax = remap.RangeInMax.TypedInputValue.Value;
            var outMin = remap.RangeOutMin.TypedInputValue.Value;
            var outMax = remap.RangeOutMax.TypedInputValue.Value;

            var inFragment = Math.Abs(inMin - inMax) < 0.001f ? 0 : (value - inMin) / (inMax - inMin);
            var outFragment = Math.Abs(outMin - outMax) < 0.001f ? 0 : (remap.Result.Value - outMin) / (outMax - outMin);

            // ill-fated attempt to visualize range mapping
            // if (false)
            // {
            //     var inGraphRect = screenRect;
            //     inGraphRect.Max.X = biasGraphRect.Min.X - size.Y * 0.2f;
            //     var minRange = MathF.Min(MathF.Min(outMin, outMax), MathF.Min(inMin, inMax));
            //     var maxRange = MathF.Max(MathF.Max(outMin, outMax), MathF.Max(inMin, inMax));
            //
            //     var normalizedIn = (value - minRange) / (maxRange - minRange);
            //     var normalizedOut = MathUtils.Remap(remap.Result.Value, minRange, maxRange, 0, 1);
            //     var padding1 = inGraphRect.GetHeight() * 0.25f;
            //     var pIn = new Vector2(inGraphRect.Min.X + 2 + padding1,
            //                           MathUtils.Lerp(inGraphRect.Max.Y, inGraphRect.Min.Y, normalizedIn));
            //
            //     var pOut = new Vector2(inGraphRect.Min.X + 2 * padding1,
            //                            MathUtils.Lerp(inGraphRect.Max.Y, inGraphRect.Min.Y, normalizedOut));
            //     drawList.AddLine(new Vector2(inGraphRect.Min.X, pIn.Y),
            //                      pIn,
            //                      UiColors.StatusAnimated, 1);
            //
            //     var normalizedInMin = (inMin - minRange) / (maxRange - minRange);
            //     var normalizedInMax = (inMax - minRange) / (maxRange - minRange);
            //     var normalizedOutMin = (outMin - minRange) / (maxRange - minRange);
            //     var normalizedOutMax = (outMax - minRange) / (maxRange - minRange);
            //
            //     drawList.AddRectFilled(new Vector2(inGraphRect.Min.X, MathUtils.Lerp(inGraphRect.Max.Y, inGraphRect.Min.Y, normalizedInMax)),
            //                            new Vector2(inGraphRect.Min.X + padding1, MathUtils.Lerp(inGraphRect.Max.Y, inGraphRect.Min.Y, normalizedInMin)),
            //                            UiColors.ForegroundFull.Fade(0.1f));
            //
            //     drawList.AddRectFilled(new Vector2(inGraphRect.Min.X + 2 * padding1, MathUtils.Lerp(inGraphRect.Max.Y, inGraphRect.Min.Y, normalizedOutMax)),
            //                            new Vector2(inGraphRect.Min.X + 3 * padding1, MathUtils.Lerp(inGraphRect.Max.Y, inGraphRect.Min.Y, normalizedOutMin)),
            //                            UiColors.ForegroundFull.Fade(0.1f));
            //
            //     drawList.AddBezierCubic(
            //                             pIn,
            //                             pIn + new Vector2(padding1 * 0.5f, 0),
            //                             pOut - new Vector2(padding1 * 0.5f, 0),
            //                             pOut,
            //                             UiColors.StatusAnimated.Fade(0.5f),
            //                             1);
            //
            //     drawList.AddLine(pOut,
            //                      pOut + new Vector2(padding1, 0),
            //                      UiColors.StatusAnimated, 1);
            // }

            drawList.PushClipRect(biasGraphRect.Min, biasGraphRect.Max, true);

            // Draw mapping graph...
            {
                const int steps = 35;
                var points = new Vector2[steps];
                var biasAndGain = remap.BiasAndGain.Value;

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
                                     UiColors.BackgroundFull.Fade(0.2f), 1);

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
            
            ValueLabel.Draw(drawList, screenRect, new Vector2(GraphRangePadding / 2, 0), remap.RangeInMax);
            ValueLabel.Draw(drawList, screenRect, new Vector2(GraphRangePadding / 2, 1), remap.RangeInMin);

            ValueLabel.Draw(drawList, screenRect, new Vector2(1 - GraphRangePadding / 2, 0), remap.RangeOutMax);
            ValueLabel.Draw(drawList, screenRect, new Vector2(1 - GraphRangePadding / 2, 1), remap.RangeOutMin);

            return SymbolChildUi.CustomUiResult.Rendered | SymbolChildUi.CustomUiResult.PreventInputLabels | SymbolChildUi.CustomUiResult.PreventOpenSubGraph |
                   SymbolChildUi.CustomUiResult.PreventTooltip;
        }

        // private const float TriangleSize = 4;
        // private static readonly Vector2[] GraphLinePoints = new Vector2[4];
    }
}