using System.Numerics;
using ImGuiNET;
using Lib.numbers.@bool.logic;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace libEditor.CustomUi;

public static class TriggerUi
{
    public static SymbolUi.Child.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect, Vector2 canvasScale)
    {
        if (instance is not Trigger trigger
            || !ImGui.IsRectVisible(screenRect.Min, screenRect.Max))
        {
            return SymbolUi.Child.CustomUiResult.None;
        }

        var dragWidth = WidgetElements.DrawOperatorDragHandle(screenRect, drawList, canvasScale);
        var colorAsVec4 = trigger.ColorInGraph.TypedInputValue.Value;
        var color = new Color(colorAsVec4);

        var activeRect = screenRect;
        activeRect.Min.X += dragWidth;

        ImGui.PushID(instance.SymbolChildId.GetHashCode());
        screenRect.Expand(-4);
        ImGui.SetCursorScreenPos(screenRect.Min);
        var symbolChild = instance.SymbolChild;
        ImGui.PushClipRect(screenRect.Min, screenRect.Max, true);

        var refValue = trigger.BoolValue.Value;
        var label = string.IsNullOrWhiteSpace(symbolChild.Name)
                        ? "Trigger"
                        : symbolChild.ReadableName;

        drawList.AddRectFilled(activeRect.Min, activeRect.Max, color.Fade(refValue ? 0.5f : 0.1f));
        var canvasScaleY = canvasScale.Y;

        var font = WidgetElements.GetPrimaryLabelFont(canvasScaleY);
        var labelColor = WidgetElements.GetPrimaryLabelColor(canvasScaleY);

        ImGui.PushFont(font);
        var labelSize = ImGui.CalcTextSize(label);

        var labelPos = activeRect.GetCenter() - labelSize/2 - new Vector2(3 * canvasScaleY,0);
        drawList.AddText(font, font.FontSize, labelPos, labelColor, label);
        ImGui.PopFont();
            
        if (!trigger.BoolValue.HasInputConnections)
        {
            var isHoveredOrActive = trigger.SymbolChildId == activeInputId ||
                                    ImGui.IsWindowHovered() && activeRect.Contains(ImGui.GetMousePos());
            if (isHoveredOrActive)
            {
                var wasChanged = false;
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    trigger.BoolValue.SetTypedInputValue(true);
                    activeInputId = trigger.SymbolChildId;
                    trigger.Result.DirtyFlag.Invalidate();
                    wasChanged = true;

                }
                else if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    activeInputId = Guid.Empty;
                    
                    trigger.BoolValue.SetTypedInputValue(false);
                    wasChanged = true;
                }

                if (wasChanged)
                {
                    trigger.Result.DirtyFlag.ForceInvalidate();
                }
            }
        }

        ImGui.PopClipRect();
        ImGui.PopID();
        return SymbolUi.Child.CustomUiResult.Rendered
               | SymbolUi.Child.CustomUiResult.PreventOpenSubGraph
               | SymbolUi.Child.CustomUiResult.PreventTooltip
               | SymbolUi.Child.CustomUiResult.PreventOpenParameterPopUp
               | SymbolUi.Child.CustomUiResult.PreventInputLabels;
    }
        
    private static Guid activeInputId;
}