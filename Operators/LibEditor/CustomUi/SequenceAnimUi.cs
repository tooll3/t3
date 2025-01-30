using System.Numerics;
using ImGuiNET;
using Lib.numbers.anim.animators;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace libEditor.CustomUi;

public static class SequenceAnimUi
{
    public static SymbolUi.Child.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect, Vector2 canvasScale)
    {
        if (!(instance is SequenceAnim sequenceAnim)
            || !ImGui.IsRectVisible(screenRect.Min, screenRect.Max))
            return SymbolUi.Child.CustomUiResult.None;

        ImGui.PushID(instance.SymbolChildId.GetHashCode());
            
        // if (RateEditLabel.Draw(ref sequenceAnim.Rate.TypedInputValue.Value,
        //                        screenRect, drawList, nameof(sequenceAnim) + " "))
        // {
        //     sequenceAnim.Rate.Input.IsDefault = false;
        //     sequenceAnim.Rate.DirtyFlag.Invalidate();
        // }

        var isEditActive = false;
        var mousePos = ImGui.GetMousePos();
        var editUnlocked = ImGui.GetIO().KeyCtrl;
        //var highlight = editUnlocked;
            
        // Speed Interaction
        //var speedRect = selectableScreenRect;
        //speedRect.Max.X = speedRect.Min.X +  speedRect.GetWidth() * 0.2f;
        //ImGui.SetCursorScreenPos(speedRect.Min);


        var h = screenRect.GetHeight();
        var w = screenRect.GetWidth();
        if (h < 10 || sequenceAnim.CurrentSequence == null || sequenceAnim.CurrentSequence.Count == 0)
        {
            return SymbolUi.Child.CustomUiResult.None;
        }
            
            
            
        if (editUnlocked)
        {
            ImGui.SetCursorScreenPos(screenRect.Min);
            ImGui.InvisibleButton("rateButton", screenRect.GetSize());
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
            }

            isEditActive = ImGui.IsItemActive();
        }
            
            

            
        drawList.PushClipRect(screenRect.Min, screenRect.Max, true);
            
        // Draw bins and window
            
        var x = screenRect.Min.X;
        var bottom = screenRect.Max.Y;

        var barCount = sequenceAnim.CurrentSequence.Count;
        var barWidth = w / barCount;
        var xPeaks = screenRect.Min.X;

        var currentIndex = (int)(sequenceAnim.NormalizedBarTime * barCount);
            
            
        ImGui.PushFont(Fonts.FontSmall);
        for (int barIndex = 0; barIndex < barCount; barIndex++)
        {
            var pMin = new Vector2(x , screenRect.Min.Y);
            var pMax = new Vector2(x + barWidth, bottom - 1);

            if (isEditActive && mousePos.X > pMin.X && mousePos.X < pMax.X)
            {
                sequenceAnim.SetStepValue(barIndex, 1-((mousePos.Y +3 - screenRect.Min.Y) / (h-6)).Clamp(0, 1));
            }
                
            var highlightFactor = barIndex == currentIndex
                                      ? 1-(sequenceAnim.NormalizedBarTime * barCount - barIndex).Clamp(0,1)
                                      : 0;

            var barIntensity = barIndex % 4 == 0 ? 0.4f : 0.1f;

            drawList.AddRectFilled(pMin,
                                   new Vector2(x + 1, bottom - 1),
                                   UiColors.WidgetBackgroundStrong.Fade(barIntensity)
                                  );

            var peak= sequenceAnim.CurrentSequence[barIndex];
            drawList.AddRectFilled(new Vector2(x + 1, bottom - peak * h - 2),
                                   new Vector2(x + barWidth, bottom-1),
                                   Color.Mix(_inactiveColor, _highlightColor,highlightFactor));
                
            drawList.AddText(pMin + new Vector2(2,0), UiColors.WidgetBackgroundStrong.Fade(barIntensity), "" + (barIndex + 1));
            x += barWidth;
            xPeaks += barWidth;
        }
        ImGui.PopFont();
            
        var min = screenRect.Min + new Vector2(sequenceAnim.NormalizedBarTime * w, 0);
        drawList.AddRectFilled(min, 
                               min + new Vector2(1, h), 
                               sequenceAnim.IsRecording ? UiColors.StatusAttention: UiColors.WidgetActiveLine);
            
        drawList.PopClipRect();
        ImGui.PopID();
        return SymbolUi.Child.CustomUiResult.Rendered 
               | SymbolUi.Child.CustomUiResult.PreventOpenSubGraph 
               | SymbolUi.Child.CustomUiResult.PreventInputLabels
               | SymbolUi.Child.CustomUiResult.PreventTooltip
               | (isEditActive ? SymbolUi.Child.CustomUiResult.IsActive : SymbolUi.Child.CustomUiResult.None);
    }

    // private static float _dragStartBias;
    // private static float _dragStartRatio;
        
    private static readonly Color _highlightColor = UiColors.StatusAnimated;
    private static readonly Color _inactiveColor = UiColors.WidgetBackgroundStrong.Fade(0.3f);
        
    //private static readonly Vector2[] GraphLinePoints = new Vector2[GraphListSteps];
    private const int GraphListSteps = 80;
}