using ImGuiNET;
using System;
using System.Numerics;
using System.Windows.Forms;
using T3.Core.Logging;
using T3.Gui.Styling;
using UiHelpers;

namespace T3.Gui
{
    static class CustomComponents
    {
        public static bool JogDial(string label, ref double delta, Vector2 size)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(1, 0.5f));
            var isActive = ImGui.Button(label + "###dummy", size);
            ImGui.PopStyleVar();
            var io = ImGui.GetIO();
            if (ImGui.IsItemActive())
            {
                var center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * 0.5f;
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                ImGui.GetForegroundDrawList().AddCircle(center, 100, Color.Gray, 50);
                isActive = true;

                var pLast = io.MousePos - io.MouseDelta - center;
                var pNow = io.MousePos - center;
                var aLast = Math.Atan2(pLast.X, pLast.Y);
                var aNow = Math.Atan2(pNow.X, pNow.Y);
                delta = aLast - aNow;
                if (delta > 1.5)
                {
                    delta -= 2 * Math.PI;
                }
                else if (delta < -1.5)
                {
                    delta += 2 * Math.PI;
                }
            }

            return isActive;
        }

        /// <summary>Draw a splitter</summary>
        /// <remarks>
        /// Take from https://github.com/ocornut/imgui/issues/319#issuecomment-147364392
        /// </remarks>
        public static bool SplitFromBottom(ref float offsetFromBottom)
        {
            const float thickness = 4;
            var hasBeenDragged = false;

            var backupPos = ImGui.GetCursorPos();

            var size = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
            var contentMin = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos();

            var pos = new Vector2(contentMin.X, contentMin.Y + size.Y - offsetFromBottom - thickness);
            ImGui.SetCursorScreenPos(pos);

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0,0,0, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.5f, 0.5f, 0.5f, 1));

            ImGui.Button("##Splitter", new Vector2(-1, thickness));

            ImGui.PopStyleColor(3);

            // Disabled for now, since Setting MouseCursor wasn't working reliably
            // if (ImGui.IsItemHovered() )
            // {
            //     //ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
            // }

            if (ImGui.IsItemActive())
            {
                if (Math.Abs(ImGui.GetIO().MouseDelta.Y) > 0)
                {
                    hasBeenDragged = true;
                    offsetFromBottom =
                        (offsetFromBottom - ImGui.GetIO().MouseDelta.Y)
                       .Clamp(0, size.Y - thickness);
                }
            }

            ImGui.SetCursorPos(backupPos);
            return hasBeenDragged;
        }

        public static bool ToggleButton(string label, ref bool isSelected, Vector2 size, bool trigger = false)
        {
            var wasSelected = isSelected;
            var clicked = false;
            if (isSelected)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, Color.Red.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Color.Red.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, Color.Red.Rgba);
            }

            if (ImGui.Button(label, size) || trigger)
            {
                isSelected = !isSelected;
                clicked = true;
            }

            if (wasSelected)
            {
                ImGui.PopStyleColor(3);
            }

            return clicked;
        }

        public static bool ToggleButton(Icon icon, string label, ref bool isSelected, Vector2 size, bool trigger = false)
        {
            var wasSelected = isSelected;
            var clicked = false;
            if (isSelected)
            {
                ImGui.PushStyleColor(ImGuiCol.Button, Color.Red.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Color.Red.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, Color.Red.Rgba);
            }

            if (CustomComponents.IconButton(icon, label, size) || trigger)
            {
                isSelected = !isSelected;
                clicked = true;
            }

            if (wasSelected)
            {
                ImGui.PopStyleColor(3);
            }

            return clicked;
        }

        public static bool IconButton(Styling.Icon icon, string label, Vector2 size)
        {
            ImGui.PushFont(Icons.IconFont);
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f, 0.3f));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);

            var clicked = ImGui.Button((char)(int)icon + "##" + label, size);

            ImGui.PopStyleVar(2);
            ImGui.PopFont();
            return clicked;
        }

        public static void ContextMenuForItem(Action drawMenuItems, string title=null, string id = "context_menu")
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6));
            
            if (ImGui.BeginPopupContextItem(id))
            {
                if (title != null)
                {
                    ImGui.PushFont(Fonts.FontLarge);
                    ImGui.Text(title);
                    ImGui.PopFont();
                }
                
                drawMenuItems?.Invoke();
                ImGui.EndPopup();
            }

            ImGui.PopStyleVar(2);
        }

        public static void DrawContextMenuForScrollCanvas(Action drawMenuContent, ref bool contextMenuIsOpen)
        {
            // This is a horrible hack to distinguish right mouse click from right mouse drag
            var rightMouseDragDelta = (ImGui.GetIO().MouseClickedPos[1] - ImGui.GetIO().MousePos).Length();
            if (!contextMenuIsOpen && rightMouseDragDelta > 3)
                return;

            if (!contextMenuIsOpen && !ImGui.IsWindowFocused())
                return;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6));

            if (ImGui.BeginPopupContextWindow("context_menu"))
            {
                ImGui.GetMousePosOnOpeningCurrentPopup();
                contextMenuIsOpen = true;

                drawMenuContent.Invoke();
                ImGui.EndPopup();
            }
            else
            {
                contextMenuIsOpen = false;
            }

            ImGui.PopStyleVar(2);
        }

        public static bool DisablableButton(string label, bool isEnabled, bool enableTriggerWithReturn = true)
        {
            if (isEnabled)
            {
                ImGui.PushFont(Fonts.FontBold);
                if (ImGui.Button(label)
                    || (enableTriggerWithReturn && ImGui.IsKeyPressed((int)Key.Return)))
                {
                    ImGui.PopFont();
                    return true;
                }

                ImGui.PopFont();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.3f, 0.1f));
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 0.15f));
                ImGui.Button(label);
                ImGui.PopStyleColor(2);
            }

            return false;
        }

        public static void HelpText(string text)
        {
            ImGui.PushFont(Fonts.FontSmall);
            ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
            ImGui.Text(text);
            ImGui.PopStyleColor();
            ImGui.PopFont();
        }
    }
}