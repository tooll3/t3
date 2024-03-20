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
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect area, Vector2 canvasScale)
        {
            if(instance is not IDescriptiveFilename descriptiveGraphNode)
                return SymbolChildUi.CustomUiResult.None;
            
            drawList.PushClipRect(area.Min, area.Max, true);
            
            // Label if instance has title
            var symbolChild = instance.Parent.Symbol.Children.Single(c => c.Id == instance.SymbolChildId);
            
            WidgetElements.DrawSmallTitle(drawList, area, !string.IsNullOrEmpty(symbolChild.Name) ? symbolChild.Name : symbolChild.Symbol.Name, canvasScale);

            var slot = descriptiveGraphNode.SourcePathSlot;
            var filePath = Path.GetFileName(slot?.TypedInputValue?.Value);
            
            WidgetElements.DrawPrimaryValue(drawList, area, filePath, canvasScale);
            
            drawList.PopClipRect();
            return SymbolChildUi.CustomUiResult.Rendered | SymbolChildUi.CustomUiResult.PreventInputLabels;
        }
    }
}