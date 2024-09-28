using System.Numerics;
using ImGuiNET;
using Lib.anim._obsolete;
using T3.Core.Operator;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace libEditor.CustomUi;

public static class Jitter2dUi
{
    public static SymbolUi.Child.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect, Vector2 canvasScale)
    {
        if (!(instance is _Jitter2d jitter2d)
            ||!ImGui.IsRectVisible(screenRect.Min, screenRect.Max))
            
            return SymbolUi.Child.CustomUiResult.None;
            
        if (WidgetElements.DrawRateLabelWithTitle(jitter2d.Rate, screenRect, drawList, nameof(jitter2d), canvasScale))
        {
            jitter2d.Rate.Input.IsDefault = false;
            jitter2d.Rate.DirtyFlag.Invalidate();
        }
        var label = $"±{jitter2d.JumpDistance.TypedInputValue.Value:0.0}";
            
        if (MicroGraph.Draw(ref jitter2d.JumpDistance.TypedInputValue.Value, 
                            ref jitter2d.Blending.TypedInputValue.Value, 
                            jitter2d.Fragment, 
                            screenRect, drawList, label))
        {
            jitter2d.Blending.Input.IsDefault = false;
            jitter2d.Blending.DirtyFlag.Invalidate();
            jitter2d.JumpDistance.Input.IsDefault = false;
            jitter2d.JumpDistance.DirtyFlag.Invalidate();
        }
        return SymbolUi.Child.CustomUiResult.Rendered  | SymbolUi.Child.CustomUiResult.PreventInputLabels;
    }
}