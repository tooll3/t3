using System.Numerics;
using ImGuiNET;
using lib.anim._obsolete;
using T3.Core.Operator;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace libEditor.CustomUi
{
    public static class _JitterUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect, Vector2 canvasScale)
        {
            if (!(instance is _Jitter jitter))
                return SymbolChildUi.CustomUiResult.None;
            
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
            return SymbolChildUi.CustomUiResult.Rendered | SymbolChildUi.CustomUiResult.PreventInputLabels;
        }
    }
}