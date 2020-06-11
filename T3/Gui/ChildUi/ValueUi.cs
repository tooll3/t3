using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.InputUi;
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
                ImGui.Text(symbolChild.Name);
            }

            ImGui.Text($"{valueInstance.Float.TypedInputValue.Value:0.00}");
            ImGui.EndGroup();
            ImGui.PopFont();
            
            ImGui.PopClipRect();
            return SymbolChildUi.CustomUiResult.Rendered;
        }
    }
}