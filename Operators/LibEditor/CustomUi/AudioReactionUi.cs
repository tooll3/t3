using System.Numerics;
using ImGuiNET;
using Lib.io.audio;
using T3.Core.Audio;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace libEditor.CustomUi;

public static class AudioReactionUi
{
    public static SymbolUi.Child.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect, Vector2 canvasScale)
    {
        if (instance is not AudioReaction audioReaction2)
            return SymbolUi.Child.CustomUiResult.None;
            
        if(!ImGui.IsRectVisible(screenRect.Min, screenRect.Max))
            return SymbolUi.Child.CustomUiResult.None;

        var h = screenRect.GetHeight();
        var w = screenRect.GetWidth();
        if (h < 10 || audioReaction2.ActiveBins == null)
        {
            return SymbolUi.Child.CustomUiResult.None;
        }

        ImGui.PushID(instance.SymbolChildId.GetHashCode());
        drawList.PushClipRect(screenRect.Min, screenRect.Max, true);

        // Draw bins and window
        var windowCenter = audioReaction2.WindowCenter.GetCurrentValue();
        var windowEdge = audioReaction2.WindowEdge.GetCurrentValue();
        var windowWidth = audioReaction2.WindowWidth.GetCurrentValue();

        var freqGraphWidth = w * 0.6f;
        var maxBars = 128;
        var x = screenRect.Min.X;
        var bottom = screenRect.Max.Y;
  
        var fftBuffer = audioReaction2.ActiveBins;
        var binCount = fftBuffer.Count;
        var barsCount = Math.Min(binCount, maxBars);
        var barWidth = freqGraphWidth / barsCount;
        var binsPerBar = (float)binCount / barsCount;
        const float valueScale = 0.5f;
            
        var inputMode = (AudioReaction.InputModes)audioReaction2.InputBand.Value.Clamp(0, Enum.GetNames(typeof(AudioReaction.InputModes)).Length);
        if (inputMode == AudioReaction.InputModes.FrequencyBandsAttacks
            || inputMode == AudioReaction.InputModes.FrequencyBands)
        {
            var xPeaks = screenRect.Min.X;
            float[] peakBands = default;
            switch (inputMode)
            {
                case AudioReaction.InputModes.FrequencyBands:
                    peakBands = AudioAnalysis.FrequencyBandPeaks;
                    break;
                case AudioReaction.InputModes.FrequencyBandsAttacks:
                    peakBands = AudioAnalysis.FrequencyBandAttackPeaks;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            for (int barIndex = 0; barIndex < peakBands.Length; barIndex++)
            {
                var peak= peakBands[barIndex];

                drawList.AddRectFilled(new Vector2(xPeaks, bottom - peak * h * valueScale - 2),
                                       new Vector2(xPeaks + barWidth, bottom-1),
                                       UiColors.BackgroundFull.Fade(0.06f));
                xPeaks += barWidth;
            }
        }
            

            
        int binIndex = 0;
        var inactiveColor = UiColors.BackgroundFull.Fade(0.2f);
        for (int barIndex = 0; barIndex < barsCount; barIndex++)
        {
            var sum = 0f;
            var count = 0;
            var maxBinForBar = barIndex * binsPerBar;
            while (binIndex <= maxBinForBar)
            {
                sum += fftBuffer[binIndex];
                binIndex++;
                count++;
            }
            sum /= count;

            var f = (float)barIndex / (barsCount - 1);
            var factor = (MathF.Abs((f - windowCenter) / windowEdge) - windowWidth / windowEdge).Clamp(0.0f, 1);
                
            drawList.AddRectFilled(new Vector2(x, bottom - sum * h * valueScale - 2),
                                   new Vector2(x + barWidth, bottom-1),
                                   Color.Mix(UiColors.StatusAnimated, inactiveColor, factor));
            x += barWidth;
                
        }




        x += barWidth;

        // Draw sum and threshold
        x += 2;
        var flashFactor = MathF.Pow((1 - (float)audioReaction2.TimeSinceLastHit * 2).Clamp(0, 1), 4);
        drawList.AddRectFilled(new Vector2(x, bottom - audioReaction2.Sum * h * valueScale),
                               new Vector2(x + w / 20, bottom),
                               Color.Mix(inactiveColor,UiColors.StatusAnimated, flashFactor));

        var thresholdY = audioReaction2.Threshold.GetCurrentValue() * h * valueScale;
        drawList.AddRectFilled(new Vector2(x, bottom - thresholdY),
                               new Vector2(x + w / 20, bottom - thresholdY+ 2),
                               UiColors.StatusAnimated);


        var w2 = windowWidth * freqGraphWidth;
        var x1 =screenRect.Min.X + windowCenter * freqGraphWidth - w2/2;
        var x2 = screenRect.Min.X +windowCenter * freqGraphWidth + w2/2;
            
        drawList.AddRectFilled(new Vector2( x1, bottom - thresholdY),
                               new Vector2(x2 + w / 20, bottom - thresholdY+ 1),
                               UiColors.ForegroundFull.Fade(0.5f));

        // Draw Spinner
        if (audioReaction2.AccumulationActive)
        {
            var center = new Vector2(screenRect.Max.X - h / 2, screenRect.Min.Y + h / 2);
                
            var a = (audioReaction2.AccumulatedLevel ) % (Math.PI * 2);
            drawList.PathClear();

            drawList.PathArcTo(center, h * 0.3f, (float)a, (float)a + 2.6f);
            drawList.PathStroke(UiColors.StatusAnimated, ImDrawFlags.None, 3);
        }

        var graphRect = screenRect;
        graphRect.Expand(-3);
        //graphRect.Min.X = graphRect.Max.X - graphRect.GetWidth() * RelativeGraphWidth;

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
                _dragStartThreshold = audioReaction2.Threshold.TypedInputValue.Value;
                _dragStartWindow = audioReaction2.WindowCenter.TypedInputValue.Value;
            }

            if (Math.Abs(dragDelta.X) > 0.5f)
            {
                audioReaction2.WindowCenter.SetTypedInputValue((_dragStartWindow + dragDelta.X / 200f).Clamp(0.001f, 1f));
                // audioReaction2.WindowCenter.DirtyFlag.Invalidate();
                // audioReaction2.WindowCenter.Input.IsDefault = false;
            }

            if (Math.Abs(dragDelta.Y) > 0.5f)
            {
                audioReaction2.Threshold.SetTypedInputValue((_dragStartThreshold - dragDelta.Y / 100f).Clamp(0.01f, 3f));
                //audioReaction2.Threshold.TypedInputValue.Value = ;
                //audioReaction2.Threshold.DirtyFlag.Invalidate();
                //audioReaction2.Threshold.Input.IsDefault = false;
            }
        }


        drawList.PopClipRect();
        ImGui.PopID();
        return SymbolUi.Child.CustomUiResult.Rendered 
               | SymbolUi.Child.CustomUiResult.PreventOpenSubGraph 
               | SymbolUi.Child.CustomUiResult.PreventInputLabels
               | SymbolUi.Child.CustomUiResult.PreventTooltip
               | (isActive ? SymbolUi.Child.CustomUiResult.IsActive : SymbolUi.Child.CustomUiResult.None);
    }

        
        
    private static float _dragStartThreshold;
    private static float _dragStartWindow;
    //private static readonly Color _highlightColor = UiColors.Animation;
    //private static readonly Color _inactiveColor = Color.Black.Fade(0.2f);
}