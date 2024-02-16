using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using lib.math.@bool;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace libEditor.CustomUi
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

            var dragWidth = WidgetElements.DrawDragIndicator(screenRect, drawList);
            var colorAsVec4 = trigger.ColorInGraph.TypedInputValue.Value;
            var color = new Color(colorAsVec4);

            var activeRect = screenRect;
            activeRect.Min.X += dragWidth;

            ImGui.PushID(instance.SymbolChildId.GetHashCode());
            screenRect.Expand(-4);
            ImGui.SetCursorScreenPos(screenRect.Min);
            var symbolChild = instance.Parent.Symbol.Children.Single(c => c.Id == instance.SymbolChildId);
            ImGui.PushClipRect(screenRect.Min, screenRect.Max, true);

            var refValue = trigger.BoolValue.Value;
            var label = string.IsNullOrEmpty(symbolChild.Name)
                            ? "Trigger"
                            : symbolChild.ReadableName;

            drawList.AddRectFilled(activeRect.Min, activeRect.Max, color.Fade(refValue ? 0.5f : 0.1f));
            var canvasScale = GraphCanvas.Current.Scale.Y;

            var font = WidgetElements.GetPrimaryLabelFont(canvasScale);
            var labelColor = WidgetElements.GetPrimaryLabelColor(canvasScale);

            ImGui.PushFont(font);
            var labelSize = ImGui.CalcTextSize(label);

            var labelPos = activeRect.GetCenter() - labelSize/2 - new Vector2(3 * canvasScale,0);
            drawList.AddText(font, font.FontSize, labelPos, labelColor, label);
            ImGui.PopFont();
            
            if (!trigger.BoolValue.IsConnected)
            {
                var isHoveredOrActive = trigger.SymbolChildId == activeInputId ||
                                        ImGui.IsWindowHovered() && activeRect.Contains(ImGui.GetMousePos());
                if (isHoveredOrActive)
                {
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        trigger.BoolValue.SetTypedInputValue(true);
                        activeInputId = trigger.SymbolChildId;
                    }
                    else if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        activeInputId = Guid.Empty;
                        trigger.BoolValue.SetTypedInputValue(false);
                    }
                }
            }

            ImGui.PopClipRect();
            ImGui.PopID();
            return SymbolChildUi.CustomUiResult.Rendered
                   | SymbolChildUi.CustomUiResult.PreventOpenSubGraph
                   | SymbolChildUi.CustomUiResult.PreventTooltip
                   | SymbolChildUi.CustomUiResult.PreventOpenParameterPopUp
                   | SymbolChildUi.CustomUiResult.PreventInputLabels;
        }
        
        private static Guid activeInputId;
    }
}