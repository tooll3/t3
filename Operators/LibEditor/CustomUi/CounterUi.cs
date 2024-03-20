using System.Numerics;
using ImGuiNET;
using lib.anim;
using T3.Core.Operator;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace libEditor.CustomUi
{
    public static class CounterUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect, Vector2 canvasScale)
        {
            if (!(instance is Counter counter)
                || !ImGui.IsRectVisible(screenRect.Min, screenRect.Max))                
                return SymbolChildUi.CustomUiResult.None;

            ImGui.PushID(instance.SymbolChildId.GetHashCode());
            if (WidgetElements.DrawRateLabelWithTitle(counter.Rate, screenRect, drawList, nameof(counter), canvasScale))
            {
                counter.Rate.Input.IsDefault = false;
                counter.Rate.DirtyFlag.Invalidate();
            }

            var inc = counter.Increment.Value;
            var label = (inc < 0 ? "-" : "+") + $"{inc:0.0}";
            if (counter.Modulo.Value > 0)
            {
                label += $" % {counter.Modulo.Value:0}";
            }
            if (MicroGraph.Draw(ref counter.Increment.TypedInputValue.Value,
                                ref counter.Blending.TypedInputValue.Value,
                                counter.Fragment,
                                screenRect, drawList, label))
            {
                counter.Blending.Input.IsDefault = false;
                counter.Blending.DirtyFlag.Invalidate();
                
                counter.Increment.Input.IsDefault = false;
                counter.Increment.DirtyFlag.Invalidate();
            }

            ImGui.PopID();
            return SymbolChildUi.CustomUiResult.Rendered 
                   | SymbolChildUi.CustomUiResult.PreventOpenSubGraph 
                   | SymbolChildUi.CustomUiResult.PreventInputLabels
                   | SymbolChildUi.CustomUiResult.PreventTooltip;
        }
    }
}