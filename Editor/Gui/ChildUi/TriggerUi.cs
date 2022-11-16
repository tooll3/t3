using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.UiHelpers;
using T3.Operators.Types.Id_0bec016a_5e1b_467a_8273_368d4d6b9935;

namespace T3.Editor.Gui.ChildUi
{
    public static class TriggerUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect)
        {
            if (instance is not Trigger trigger
                || !ImGui.IsRectVisible(screenRect.Min, screenRect.Max))
            {
                return SymbolChildUi.CustomUiResult.None;
            }

            ImGui.PushID(instance.SymbolChildId.GetHashCode());

            screenRect.Expand(-4);
            
            ImGui.SetCursorScreenPos(screenRect.Min + new Vector2(2, 2));
            var symbolChild = instance.Parent.Symbol.Children.Single(c => c.Id == instance.SymbolChildId);
            ImGui.PushClipRect(screenRect.Min, screenRect.Max, true);

            var label = string.IsNullOrEmpty(symbolChild.Name)
                            ? "Trigger"
                            : symbolChild.ReadableName;
            
            if (ImGui.Button(label, screenRect.GetSize()))
            {
                trigger.Activate();
            }

            ImGui.PopClipRect();
            ImGui.PopID();
            return SymbolChildUi.CustomUiResult.Rendered | SymbolChildUi.CustomUiResult.PreventInputLabels | SymbolChildUi.CustomUiResult.PreventOpenSubGraph;
        }
    }
}