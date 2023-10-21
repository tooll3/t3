using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using T3.Operators.Types.Id_ed0f5188_8888_453e_8db4_20d87d18e9f4;
using Icon = T3.Editor.Gui.Styling.Icon;

namespace T3.Editor.Gui.ChildUi
{
    public static class BooleanUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect)
        {
            if (!(instance is Boolean boolean)
                || !ImGui.IsRectVisible(screenRect.Min, screenRect.Max))
                return SymbolChildUi.CustomUiResult.None;

            ImGui.PushID(instance.SymbolChildId.GetHashCode());

            ImGui.SetCursorScreenPos(screenRect.Min + new Vector2(2, 2));
            var symbolChild = instance.Parent.Symbol.Children.Single(c => c.Id == instance.SymbolChildId);
            ImGui.PushClipRect(screenRect.Min, screenRect.Max, true);

            var refValue = boolean.BoolValue.Value; // we reference here to show correct state when connected

            if (CustomComponents.ToggleIconButton(Icon.Checkmark, "", ref refValue, new Vector2(20, 20)))
            {
                if (!boolean.BoolValue.IsConnected)
                {
                    boolean.BoolValue.TypedInputValue.Value = !boolean.BoolValue.TypedInputValue.Value;
                }

                boolean.BoolValue.Input.IsDefault = false;
                boolean.BoolValue.DirtyFlag.Invalidate();
            }

            ImGui.SameLine();
            var label = string.IsNullOrEmpty(symbolChild.Name)
                            ? refValue ? "True" : "False"
                            : symbolChild.ReadableName;
            ImGui.TextUnformatted(label);
            ImGui.PopClipRect();
            ImGui.PopID();
            return SymbolChildUi.CustomUiResult.Rendered | SymbolChildUi.CustomUiResult.PreventInputLabels;
        }
    }
}