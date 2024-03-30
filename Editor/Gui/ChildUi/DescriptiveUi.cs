using System.IO;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.ChildUi
{
    public static class DescriptiveUi
    {
        internal static readonly DrawChildUiDelegate DrawChildUiDelegate = DrawChildUi;
        public static SymbolUi.Child.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect area, Vector2 canvasScale)
        {
            if(instance is not IDescriptiveFilename descriptiveGraphNode)
                return SymbolUi.Child.CustomUiResult.None;
            
            drawList.PushClipRect(area.Min, area.Max, true);
            
            // Label if instance has title
            var symbolChild = instance.SymbolChild;
            
            WidgetElements.DrawSmallTitle(drawList, area, !string.IsNullOrEmpty(symbolChild.Name) ? symbolChild.Name : symbolChild.Symbol.Name, canvasScale);

            var slot = descriptiveGraphNode.SourcePathSlot;
            var filePath = Path.GetFileName(slot?.TypedInputValue?.Value);
            
            WidgetElements.DrawPrimaryValue(drawList, area, filePath, canvasScale);
            
            drawList.PopClipRect();
            return SymbolUi.Child.CustomUiResult.Rendered | SymbolUi.Child.CustomUiResult.PreventInputLabels;
        }
    }
}