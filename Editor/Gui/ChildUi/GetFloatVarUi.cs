using System.Linq;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using T3.Operators.Types.Id_e6072ecf_30d2_4c52_afa1_3b195d61617b;

namespace T3.Editor.Gui.ChildUi
{
    public static class GetFloatVarUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance1, ImDrawListPtr drawList, ImRect area)
        {
            if (!(instance1 is GetFloatVar instance))
                return SymbolChildUi.CustomUiResult.PreventOpenSubGraph;

            var symbolChild = instance.Parent.Symbol.Children.Single(c => c.Id == instance.SymbolChildId);
            drawList.PushClipRect(area.Min, area.Max, true);

            var value = instance.Result.Value;

            if (!string.IsNullOrEmpty(symbolChild.Name))
            {
                WidgetElements.DrawPrimaryTitle(drawList, area, symbolChild.Name);
            }
            else
            {
                WidgetElements.DrawPrimaryTitle(drawList, area, "Get " + instance.VariableName.TypedInputValue.Value);
            }

            WidgetElements.DrawSmallValue(drawList, area, $"{value:0.000}");

            drawList.PopClipRect();
            return SymbolChildUi.CustomUiResult.Rendered | SymbolChildUi.CustomUiResult.PreventInputLabels | SymbolChildUi.CustomUiResult.PreventOpenSubGraph;
        }
    }
}