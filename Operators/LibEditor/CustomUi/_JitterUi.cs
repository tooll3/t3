using System.Numerics;
using ImGuiNET;
using Lib.numbers.anim._obsolete;
using T3.Core.Operator;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace libEditor.CustomUi;

public static class _JitterUi
{
    public static SymbolUi.Child.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect, Vector2 canvasScale)
    {
        if (!(instance is _Jitter jitter))
            return SymbolUi.Child.CustomUiResult.None;
            
        if (WidgetElements.DrawRateLabelWithTitle(jitter.Rate, screenRect, drawList, nameof(jitter), canvasScale))
        {
            jitter.Rate.Input.IsDefault = false;
            jitter.Rate.DirtyFlag.Invalidate();
        }
        var label = $"±{jitter.JumpDistance.TypedInputValue.Value:0.0}";
            
        if (MicroGraph.Draw(ref jitter.JumpDistance.TypedInputValue.Value, 
                            ref jitter.Blending.TypedInputValue.Value, 
                            jitter.Fragment, 
                            screenRect, drawList, label))
        {
            jitter.Blending.Input.IsDefault = false;
            jitter.Blending.DirtyFlag.Invalidate();
            jitter.JumpDistance.Input.IsDefault = false;
            jitter.JumpDistance.DirtyFlag.Invalidate();
        }
        return SymbolUi.Child.CustomUiResult.Rendered | SymbolUi.Child.CustomUiResult.PreventInputLabels;
    }
}