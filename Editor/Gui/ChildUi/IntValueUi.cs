using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using T3.Operators.Types.Id_cc07b314_4582_4c2c_84b8_bb32f59fc09b;

namespace T3.Editor.Gui.ChildUi
{
    public static class IntValueUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect)
        {
            if (!(instance is IntValue intValueInstance))
                return SymbolChildUi.CustomUiResult.None;

            var symbolChild = intValueInstance.Parent.Symbol.Children.Single(c => c.Id == intValueInstance.SymbolChildId);
            ImGui.PushClipRect(selectableScreenRect.Min, selectableScreenRect.Max, true);
            
            var h = selectableScreenRect.GetHeight();
            var font = h > 40
                           ? Fonts.FontLarge
                           : (h > 25
                                  ? Fonts.FontNormal
                                  : Fonts.FontSmall);

            ImGui.PushFont(font);
            ImGui.SetCursorScreenPos(selectableScreenRect.Min + new Vector2(10,0));
            ImGui.BeginGroup();
            if (!string.IsNullOrEmpty(symbolChild.Name))
            {
                ImGui.TextUnformatted(symbolChild.Name);
            }

            var isAnimated = instance.Parent?.Symbol.Animator.IsInputSlotAnimated(intValueInstance.Int)??false;

            var value = (isAnimated || intValueInstance.Int.HasInputConnections) 
                            ? intValueInstance.Int.Value 
                            : intValueInstance.Int.TypedInputValue.Value;
            
            ImGui.TextUnformatted($"{value:0}");
            ImGui.EndGroup();
            ImGui.PopFont();
            
            ImGui.PopClipRect();
            return SymbolChildUi.CustomUiResult.Rendered 
                   | SymbolChildUi.CustomUiResult.PreventOpenSubGraph 
                   | SymbolChildUi.CustomUiResult.PreventInputLabels
                   | SymbolChildUi.CustomUiResult.PreventTooltip;
        }
    }
}