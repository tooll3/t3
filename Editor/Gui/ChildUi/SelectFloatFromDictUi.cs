using System.Linq;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using T3.Operators.Types.Id_fd5467c7_c75d_4755_8885_fd1ff1f07c95;

namespace T3.Editor.Gui.ChildUi
{
    public static class SelectFloatFromDictUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance1, ImDrawListPtr drawList, ImRect area)
        {
            if (instance1 is not SelectFloatFromDict instance)
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
                WidgetElements.DrawPrimaryTitle(drawList, area, instance.Select.TypedInputValue.Value);
            }

            WidgetElements.DrawSmallValue(drawList, area, $"{value:0.00}");

            drawList.PopClipRect();
            return SymbolChildUi.CustomUiResult.Rendered | SymbolChildUi.CustomUiResult.PreventInputLabels | SymbolChildUi.CustomUiResult.PreventOpenSubGraph;
        }
    }
}