using System.Numerics;
using ImGuiNET;
using Lib.numbers.data.utils;
using T3.Core.Operator;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.ChildUi;

public static class SelectFloatFromDictUi
{
    public static SymbolUi.Child.CustomUiResult DrawChildUi(Instance instance1, ImDrawListPtr drawList, ImRect area, Vector2 canvasScale)
    {
        if (instance1 is not SelectFloatFromDict instance)
            return SymbolUi.Child.CustomUiResult.PreventOpenSubGraph;

        var symbolChild = instance.Parent.Symbol.Children[instance.SymbolChildId];
        drawList.PushClipRect(area.Min, area.Max, true);

        var value = instance.Result.Value;

        if (!string.IsNullOrEmpty(symbolChild.Name))
        {
            WidgetElements.DrawPrimaryTitle(drawList, area, symbolChild.Name, canvasScale);
        }
        else
        {
            WidgetElements.DrawPrimaryTitle(drawList, area, instance.Select.TypedInputValue.Value, canvasScale);
        }

        WidgetElements.DrawSmallValue(drawList, area, $"{value:0.00}", canvasScale);

        drawList.PopClipRect();
        return SymbolUi.Child.CustomUiResult.Rendered | SymbolUi.Child.CustomUiResult.PreventInputLabels | SymbolUi.Child.CustomUiResult.PreventOpenSubGraph;
    }
}