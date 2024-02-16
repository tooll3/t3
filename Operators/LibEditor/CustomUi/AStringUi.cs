using System;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using String = lib.@string.AString;

namespace libEditor.CustomUi
{
    public static class AStringUi
    {
        /// <summary>
        /// Draws a custom ui that allows direct editing of strings within the graph 
        /// </summary>
        /// <remarks>
        /// The implementation is kind of ugly, mostly because I had a very had time to
        /// detect if the control loses focus. The normal candidates like IsItemDeactivated() didn't
        /// catch all cases. I'm pretty sure that detected all clicks is no ideal either.
        ///
        /// Using an invisibleButton interfered with the drag interaction of the node.
        /// </remarks>
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect)
        {
            if (!(instance is String stringInstance))
                return SymbolChildUi.CustomUiResult.None;

            if (stringInstance.InputString.IsConnected)
                return SymbolChildUi.CustomUiResult.None;

            var dragWidth = WidgetElements.DrawDragIndicator(screenRect, drawList);
            var usableArea = screenRect;
            usableArea.Min.X += dragWidth;

            ImGui.PushID(instance.SymbolChildId.GetHashCode());

            ImGui.PushFont(GraphCanvas.Current.Scale.X < 2
                               ? Fonts.FontSmall
                               : GraphCanvas.Current.Scale.X < 4 
                                   ? Fonts.FontNormal
                                   : Fonts.FontLarge);

            // Draw edit window
            if (instance.SymbolChildId == _focusedInstanceId)
            {
                usableArea.Max.X -= 10f; // Keep some padding for resize handle
                usableArea.Expand(-3);
                ImGui.SetKeyboardFocusHere();
                ImGui.SetCursorScreenPos(usableArea.Min);
                if (ImGui.InputTextMultiline("##str", ref stringInstance.InputString.TypedInputValue.Value, 16368, usableArea.GetSize(),
                                             ImGuiInputTextFlags.None))
                {
                    stringInstance.InputString.Input.IsDefault = false;
                    stringInstance.InputString.DirtyFlag.Invalidate();
                }

                var clickedOutside = ImGui.IsMouseReleased(ImGuiMouseButton.Left) && !usableArea.Contains(ImGui.GetMousePos());
                if (ImGui.IsItemDeactivated() || clickedOutside)
                {
                    _focusedInstanceId = Guid.Empty;
                }
            }
            // Draw viewer
            else
            {
                //Log.Debug("hovered " + ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows | ImGuiHoveredFlags.AllowWhenBlockedByPopup | ImGuiHoveredFlags.AllowWhenDisabled ) + " focus" + ImGui.IsWindowFocused(), stringInstance);
                usableArea.Expand(GraphCanvas.Current.Scale.X < 0.75f ? 0 : -4);
                if (usableArea.Contains(ImGui.GetMousePos())
                    && (ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows) || ImGui.IsWindowFocused())
                    && ImGui.IsMouseReleased(ImGuiMouseButton.Left)
                    && ImGui.GetMouseDragDelta(ImGuiMouseButton.Left, 0).Length() < UserSettings.Config.ClickThreshold)
                {
                    _focusedInstanceId = instance.SymbolChildId;
                }

                var v = stringInstance.InputString.TypedInputValue.Value;
                if (!string.IsNullOrEmpty(v))
                {
                    ImGui.PushClipRect(usableArea.Min, usableArea.Max, true);
                    var color = TypeUiRegistry.Entries[typeof(string)];
                    ImGui.GetWindowDrawList().AddText(usableArea.Min, ImGui.ColorConvertFloat4ToU32(color.Color.Rgba), v);
                    ImGui.PopClipRect();
                }
            }

            ImGui.PopFont();
            ImGui.PopID();
            return SymbolChildUi.CustomUiResult.Rendered | SymbolChildUi.CustomUiResult.PreventOpenSubGraph | SymbolChildUi.CustomUiResult.PreventTooltip | SymbolChildUi.CustomUiResult.PreventOpenParameterPopUp;
        }

        private static Guid _focusedInstanceId;
    }
}