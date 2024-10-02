using System.Linq;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using T3.Operators.Types.Id_470db771_c7f2_4c52_8897_d3a9b9fc6a4e;

namespace T3.Editor.Gui.ChildUi
{
    public static class GetIntVarUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance1, ImDrawListPtr drawList, ImRect area)
        {
            if (instance1 is not GetIntVar instance)
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

            WidgetElements.DrawSmallValue(drawList, area, $"{value:0}");

            drawList.PopClipRect();
            return SymbolChildUi.CustomUiResult.Rendered | SymbolChildUi.CustomUiResult.PreventInputLabels | SymbolChildUi.CustomUiResult.PreventOpenSubGraph;
        }
    }
}