using ImGuiNET;
using T3.Core.Utils;
using T3.Editor.Gui.MagGraph.Model;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.MagGraph.Ui;

internal sealed partial class MagGraphCanvas
{
    private void DrawAnnotation(MagGraphAnnotation magAnnotation, ImDrawListPtr drawList, GraphUiContext context)
    {
        var canvas = context.Canvas;
        var area = ImRect.RectWithSize(magAnnotation.Annotation.PosOnCanvas, 
                                       magAnnotation.Annotation.Size);
        
        if (!IsRectVisible(area))
            return;
        
        var pMin = TransformPosition(magAnnotation.DampedPosOnCanvas);
        var pMax = TransformPosition(magAnnotation.DampedPosOnCanvas + magAnnotation.DampedSize);
        
        drawList.AddRectFilled(pMin,
                               pMax,
                               UiColors.BackgroundFull.Fade(0.2f), 
                               3 * context.Canvas.CanvasScale);

        var borderColor = context.Selector.IsNodeSelected(magAnnotation)
                              ? UiColors.ForegroundFull
                              : UiColors.ForegroundFull.Fade(0.1f);
        
        drawList.AddRect(pMin,
                         pMax,
                         borderColor, 
                         3 * context.Canvas.CanvasScale);
        
        // Label
        if(!string.IsNullOrEmpty(magAnnotation.Annotation.Title)) {
            var canvasScale = canvas.Scale.X;
            var font = magAnnotation.Annotation.Title.StartsWith("# ") ? Fonts.FontLarge: Fonts.FontNormal;
            var fade = MathUtils.SmootherStep(0.25f, 0.6f, canvasScale);
            drawList.PushClipRect(pMin, pMax, true);
            var labelPos = pMin + new Vector2(8, 6) * T3Ui.DisplayScaleFactor;

            var fontSize = canvasScale > 1 
                               ? font.FontSize
                               : canvasScale >  Fonts.FontSmall.Scale / Fonts.FontNormal.Scale
                                   ? font.FontSize
                                   : font.FontSize * canvasScale;
            drawList.AddText(font,
                             fontSize,
                             labelPos,
                             ColorVariations.OperatorLabel.Apply(magAnnotation.Annotation.Color.Fade(fade)),
                             magAnnotation.Annotation.Title);
            drawList.PopClipRect();
        }        
    }
}