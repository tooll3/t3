using System.Linq;
using System.Numerics;
using Editor.Gui;
using Editor.Gui.Styling;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Editor.Gui.UiHelpers;
using UiHelpers;

namespace T3.Editor.Gui.ChildUi
{
    public static class DescriptiveUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect)
        {
            if (!(instance is IDescriptiveGraphNode descriptiveGraphNode) )
                return SymbolChildUi.CustomUiResult.None;

            var descriptiveString = descriptiveGraphNode.GetDescriptiveString();
            if (string.IsNullOrEmpty(descriptiveString))
            {
                return SymbolChildUi.CustomUiResult.None;
            }
            
            var symbolChild = instance.Parent.Symbol.Children.Single(c => c.Id == instance.SymbolChildId);
            ImGui.PushClipRect(selectableScreenRect.Min, selectableScreenRect.Max, true);
            
            var h = selectableScreenRect.GetHeight();
            var font = h > 50
                           ? Fonts.FontLarge
                           : (h > 25
                                  ? Fonts.FontNormal
                                  : Fonts.FontSmall);

            ImGui.PushFont(font);
            ImGui.SetCursorScreenPos(selectableScreenRect.Min + new Vector2(10,0));
            ImGui.BeginGroup();
            if (!string.IsNullOrEmpty(symbolChild.Name))
            {
                var isRenamed = !string.IsNullOrEmpty(symbolChild.Name);
                ImGui.TextUnformatted(isRenamed
                                          ? $"\"{symbolChild.ReadableName}\""
                                          : symbolChild.ReadableName);
            }
            ImGui.TextUnformatted(descriptiveString);

            ImGui.EndGroup();
            ImGui.PopFont();
            
            ImGui.PopClipRect();
            return SymbolChildUi.CustomUiResult.Rendered | SymbolChildUi.CustomUiResult.PreventInputLabels;
        }
    }
}