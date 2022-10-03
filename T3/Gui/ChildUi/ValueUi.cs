using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Gui.Styling;
using T3.Operators.Types.Id_5d7d61ae_0a41_4ffa_a51d_93bab665e7fe;
using UiHelpers;

namespace T3.Gui.ChildUi
{
    public static class ValueUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect)
        {
            if (!(instance is Value valueInstance))
                return SymbolChildUi.CustomUiResult.None;

            var symbolChild = valueInstance.Parent.Symbol.Children.Single(c => c.Id == valueInstance.SymbolChildId);
            ImGui.PushClipRect(selectableScreenRect.Min, selectableScreenRect.Max, true);
            
            ImGui.SetCursorScreenPos(selectableScreenRect.Min + new Vector2(10,0));
            ImGui.BeginGroup();
            
            // Label if instance has title
            if (!string.IsNullOrEmpty(symbolChild.Name))
            {
                ImGui.TextUnformatted(symbolChild.Name);
            }
            
            var h = selectableScreenRect.GetHeight();
            var font = h > 40
                           ? Fonts.FontLarge
                           : (h > 25
                                  ? Fonts.FontNormal
                                  : Fonts.FontSmall);

            ImGui.PushFont(font);
            ImGui.TextUnformatted($"{valueInstance.Float.Value:0.000}");
            ImGui.PopFont();
            
            ImGui.EndGroup();
            
            ImGui.PopClipRect();
            return SymbolChildUi.CustomUiResult.Rendered | SymbolChildUi.CustomUiResult.PreventInputLabels;
        }
    }
}