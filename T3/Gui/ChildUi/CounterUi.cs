using System;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.Operator;
using T3.Gui.ChildUi.Animators;
using T3.Gui.Styling;
using T3.Operators.Types.Id_11882635_4757_4cac_a024_70bb4e8b504c;
using T3.Operators.Types.Id_23794a1f_372d_484b_ac31_9470d0e77819;
using UiHelpers;

namespace T3.Gui.ChildUi
{
    public static class CounterUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect)
        {
            if (!(instance is Counter counter)
                || !ImGui.IsRectVisible(screenRect.Min, screenRect.Max))                
                return SymbolChildUi.CustomUiResult.None;

            ImGui.PushID(instance.SymbolChildId.GetHashCode());
            if (RateEditLabel.Draw(ref counter.Rate.TypedInputValue.Value,
                                   screenRect, drawList, nameof(counter)))
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
            return SymbolChildUi.CustomUiResult.Rendered |  SymbolChildUi.CustomUiResult.PreventInputLabels;
        }
    }
}