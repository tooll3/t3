using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core;
using T3.Core.IO;
using T3.Gui.Graph;
using T3.Gui.InputUi;
using T3.Gui.Interaction;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;
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
            const float thickness = 3;
            var hasBeenDragged = false;

            var backupPos = ImGui.GetCursorPos();

            var size = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
            var contentMin = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos();

            var pos = new Vector2(contentMin.X, contentMin.Y + size.Y - offsetFromBottom - thickness);
            ImGui.SetCursorScreenPos(pos);

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0, 0, 0, 1));
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
                ImGui.PushStyleColor(ImGuiCol.Button, Color.Gray.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Color.Gray.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, Color.Gray.Rgba);
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

        public static bool ToggleIconButton(Icon icon, string label, ref bool isSelected, Vector2 size, bool trigger = false)
        {
            var wasSelected = isSelected;
            var clicked = false;

            //ImGui.PushStyleColor(ImGuiCol.Text, isSelected ? new Color(1f).Rgba : new Color(0.3f));
            ImGui.PushStyleColor(ImGuiCol.Text, isSelected ? new Color(1f, 1, 1f, 1f).Rgba : new Color(0, 0, 0, 1f));
            // ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Color.Red.Rgba);
            // ImGui.PushStyleColor(ImGuiCol.ButtonActive, Color.Red.Rgba);

            if (CustomComponents.IconButton(icon, label, size) || trigger)
            {
                isSelected = !isSelected;
                clicked = true;
            }

            ImGui.PopStyleColor(1);

            return clicked;
        }

        public static bool IconButton(Icon icon, string label, Vector2 size)
        {
            ImGui.PushFont(Icons.IconFont);
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f, 0.5f));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
            
            var clicked = ImGui.Button((char)(int)icon + label, size);

            ImGui.PopStyleVar(2);
            ImGui.PopFont();
            return clicked;
        }

        public static void ContextMenuForItem(Action drawMenuItems, string title = null, string id = "context_menu", ImGuiPopupFlags flags = ImGuiPopupFlags.MouseButtonRight)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6));

            if (ImGui.BeginPopupContextItem(id, flags))
            {
                if (title != null)
                {
                    ImGui.PushFont(Fonts.FontLarge);
                    ImGui.TextUnformatted(title);
                    ImGui.PopFont();
                }

                drawMenuItems?.Invoke();
                ImGui.EndPopup();
            }

            ImGui.PopStyleVar(2);
        }

        public static void PopUp(Action drawContent, string id = "context_menu")
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6));

            if (ImGui.BeginPopupContextItem(id))
            {
                drawContent?.Invoke();
                ImGui.EndPopup();
            }

            ImGui.PopStyleVar(2);
        }

        public static void DrawContextMenuForScrollCanvas(Action drawMenuContent, ref bool contextMenuIsOpen)
        {
            // This is a horrible hack to distinguish right mouse click from right mouse drag
            //var rightMouseDragDelta = (ImGui.GetIO().MouseClickedPos[1] - ImGui.GetIO().MousePos).Length();
            var wasDraggingRight = ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Length() > UserSettings.Config.ClickThreshold;

            if (!contextMenuIsOpen)
            {
                if (wasDraggingRight)
                    return;

                if (!ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup))
                    return;
            }

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

        public static bool DisablableButton(string label, bool isEnabled, bool enableTriggerWithReturn = false)
        {
            if (isEnabled)
            {
                ImGui.PushFont(Fonts.FontBold);
                if (ImGui.Button(label)
                    || (enableTriggerWithReturn && ImGui.IsKeyPressed((ImGuiKey)Key.Return)))
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
            ImGui.TextUnformatted(text);
            ImGui.PopStyleColor();
            ImGui.PopFont();
        }

        /// <summary>
        /// A small label that can be used to structure context menus
        /// </summary>
        public static void HintLabel(string label)
        {
            ImGui.PushFont(Fonts.FontSmall);
            ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
            ImGui.TextUnformatted(label);
            ImGui.PopStyleColor();
            ImGui.PopFont();
        }

        public static void FillWithStripes(ImDrawListPtr drawList, ImRect areaOnScreen, float patternWidth = 16)
        {
            drawList.PushClipRect(areaOnScreen.Min, areaOnScreen.Max, true);
            var lineColor = new Color(0f, 0f, 0f, 0.2f);
            var stripeOffset = GraphCanvas.Current == null ? patternWidth : (patternWidth / 2 * GraphCanvas.Current.Scale.X);
            var lineWidth = stripeOffset / 2.7f;

            var h = areaOnScreen.GetHeight();
            var stripeCount = (int)((areaOnScreen.GetWidth() + h + 3 * lineWidth) / stripeOffset);
            var p = areaOnScreen.Min - new Vector2(h + lineWidth, +lineWidth);
            var offset = new Vector2(h + 2 * lineWidth,
                                     h + 2 * lineWidth);

            for (var i = 0; i < stripeCount; i++)
            {
                drawList.AddLine(p, p + offset, lineColor, lineWidth);
                p.X += stripeOffset;
            }

            drawList.PopClipRect();
        }

        public static void DrawSplitter(bool splitVertically, float thickness, ref float size0, ref float size1, float minSize0, float minSize1)
        {
            var backupPos = ImGui.GetCursorPos();
            if (splitVertically)
                ImGui.SetCursorPosY(backupPos.Y + size0);
            else
                ImGui.SetCursorPosX(backupPos.X + size0);

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 1));

            // We don't draw while active/pressed because as we move the panes the splitter button will be 1 frame late
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0, 0, 0, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.6f, 0.6f, 0.10f));
            ImGui.Button("##Splitter", new Vector2(!splitVertically ? thickness : -1.0f, splitVertically ? thickness : -1.0f));
            ImGui.PopStyleColor(3);

            ImGui.SetItemAllowOverlap(); // This is to allow having other buttons OVER our splitter. 

            if (ImGui.IsAnyItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
            }

            if (ImGui.IsItemActive())
            {
                var mouseDelta = splitVertically ? ImGui.GetIO().MouseDelta.Y : ImGui.GetIO().MouseDelta.X;

                //// Minimum pane size
                //if (mouse_delta < min_size0 - size0)
                //    mouse_delta = min_size0 - size0;
                //if (mouse_delta > size1 - min_size1)
                //    mouse_delta = size1 - min_size1;

                // Apply resize
                size0 += mouseDelta;
                size1 -= mouseDelta;
            }

            ImGui.SetCursorPos(backupPos);
        }

        public static void DrawContentRegion()
        {
            ImGui.GetForegroundDrawList().AddRect(
                                                  ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos(),
                                                  ImGui.GetWindowContentRegionMax() + ImGui.GetWindowPos(),
                                                  Color.White);
        }

        

        // public static void ToggleButton(string str_id, ref bool v)
        // {
        //     var p = ImGui.GetCursorScreenPos();
        //     var drawList = ImGui.GetWindowDrawList();
        //
        //     var height = ImGui.GetFrameHeight();
        //     var width = height * 1.55f;
        //     var radius = height * 0.50f;
        //
        //     ImGui.InvisibleButton(str_id, new Vector2(width, height));
        //     if (ImGui.IsItemClicked())
        //         v = !v;
        //
        //     var t = v ? 1.0f : 0.0f;
        //
        //     //ImGuiContext & g = *GImGui;
        //     //var g = ImGui.GetCurrentContext();
        //     //float ANIM_SPEED = 0.08f;
        //     //if (g.LastActiveId == g.CurrentWindow->GetID(str_id))// && g.LastActiveIdTimer < ANIM_SPEED)
        //     //{
        //     //    float t_anim = ImSaturate(g.LastActiveIdTimer / ANIM_SPEED);
        //     //    t = v ? (t_anim) : (1.0f - t_anim);
        //     //}
        //
        //     var colBg = ImGui.IsItemHovered()
        //                     ? Color.White
        //                     : Color.Red;
        //
        //     drawList.AddRectFilled(p, new Vector2(p.X + width, p.Y + height), colBg, height * 0.5f);
        //     drawList.AddCircleFilled(new Vector2(p.X + radius + t * (width - radius * 2.0f), p.Y + radius), radius - 1.5f, Color.White);
        // }

        public static bool EmptyWindowMessage(string message, string buttonLabel = null)
        {
            var center = (ImGui.GetWindowContentRegionMax() + ImGui.GetWindowContentRegionMin()) / 2 + ImGui.GetWindowPos();
            var lines = message.Split('\n').ToArray();

            var lineCount = lines.Length;
            if (!string.IsNullOrEmpty(buttonLabel))
                lineCount++;
            
            var textLineHeight = ImGui.GetTextLineHeight();
            var y = center.Y - lineCount * textLineHeight / 2;
            var drawList = ImGui.GetWindowDrawList();

            foreach (var line in lines)
            {
                var textSize = ImGui.CalcTextSize(line);
                var position = new Vector2(center.X - textSize.X / 2, y);
                drawList.AddText(position, EmptyMessageColor, line);
                y += textLineHeight;
            }

            
            if (!string.IsNullOrEmpty(buttonLabel))
            {
                y += 10;
                var style = ImGui.GetStyle(); 
                var textSize = ImGui.CalcTextSize(buttonLabel) + style.FramePadding;
                var position = new Vector2(center.X - textSize.X / 2, y);
                ImGui.SetCursorScreenPos(position);
                return ImGui.Button(buttonLabel);
            }
            return false;
        }

        private static Color EmptyMessageColor = new Color(0.3f);

        public static void TooltipForLastItem(string message, string additionalNotes = null, bool useHoverDelay = true)
        {
            if (!ImGui.IsAnyItemHovered())
            {
                _hoverStartTime = -1;
                return;
            }

            if (!ImGui.IsItemHovered())
                return;

            if (_hoverStartTime <= 0)
                _hoverStartTime = ImGui.GetTime();

            var hoverDuration = ImGui.GetTime() - _hoverStartTime;
            if (useHoverDelay && !(hoverDuration > UserSettings.Config.TooltipDelay))
                return;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(300);
            ImGui.TextUnformatted(message);
            if (!string.IsNullOrEmpty(additionalNotes))
            {
                ImGui.TextColored(Color.Gray, additionalNotes);
            }
            ImGui.PopTextWrapPos();

            ImGui.EndTooltip();
            ImGui.PopStyleVar();
        }

        private static double _hoverStartTime;

        public static bool DrawSegmentedToggle(ref int currentIndex, List<string> options)
        {
            var changed = false;
            for (var index = 0; index < options.Count; index++)
            {
                var isActive = currentIndex == index;
                var option = options[index];

                ImGui.SameLine(0);
                ImGui.PushFont(isActive ? Fonts.FontBold : Fonts.FontNormal);
                ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Color.White.Fade(0.1f).Rgba);
                ImGui.PushStyleColor(ImGuiCol.Text, isActive ? Color.White : Color.White.Fade(0.5f).Rgba);

                if (ImGui.Button(option))
                {
                    if (!isActive)
                    {
                        currentIndex = index;
                        changed = true;
                    }
                }

                ImGui.PopFont();
                ImGui.PopStyleColor(3);
            }

            return changed;
        }


        public static bool FloatValueEdit(string label, ref float value, float min= float.NegativeInfinity, float max= float.PositiveInfinity, float scale= 0)
        {
            var labelSize = ImGui.CalcTextSize(label);
            const float leftPadding = 200;
            var p = ImGui.GetCursorPos();
            ImGui.SetCursorPosX( MathF.Max(leftPadding - labelSize.X,0)+10);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(label);
            ImGui.SetCursorPos(p);
            
            ImGui.SameLine();
            ImGui.SetCursorPosX(leftPadding + 20);
            var size = new Vector2(150, ImGui.GetFrameHeight());
            ImGui.PushID(label);
            var result = SingleValueEdit.Draw(ref value, size, min, max);
            var modified = (result & InputEditStateFlags.Modified) != InputEditStateFlags.Nothing;
            ImGui.PopID();
            return modified;
        }

        public static bool IntValueEdit(string label, ref int value, int min = int.MinValue, int max = int.MaxValue, float scale = 1)
        {
            var labelSize = ImGui.CalcTextSize(label);
            const float leftPadding = 200;
            var p = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(MathF.Max(leftPadding - labelSize.X, 0) + 10);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(label);
            ImGui.SetCursorPos(p);

            ImGui.SameLine();
            ImGui.SetCursorPosX(leftPadding + 20);
            var size = new Vector2(150, ImGui.GetFrameHeight());
            ImGui.PushID(label);
            var result = SingleValueEdit.Draw(ref value, size, min, max, true, scale);
            var modified = (result & InputEditStateFlags.Modified) != InputEditStateFlags.Nothing;
            ImGui.PopID();
            return modified;
        }

        public static bool StringValueEdit(string label, ref string value)
        {
            const float leftPadding = 200;
            var labelSize = ImGui.CalcTextSize(label);

            var p = ImGui.GetCursorPos();
            ImGui.SetCursorPosX( MathF.Max(leftPadding - labelSize.X,0)+10);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(label);
            ImGui.SetCursorPos(p);
            
            ImGui.SameLine();
            ImGui.SetCursorPosX(leftPadding + 20);
            //var size = new Vector2(150, ImGui.GetFrameHeight());
            
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X- 50);
            var modified = ImGui.InputText("##" + label, ref value, 1000);
            return modified;
        }

        public static bool DrawEnumSelector<T>(ref int index, string label)
        {
            // Label
            var labelSize = ImGui.CalcTextSize(label);

            var p = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(MathF.Max(200 - labelSize.X, 0) + 10);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(label);
            ImGui.SetCursorPos(p);

            // Dropdown
            ImGui.SameLine();
            ImGui.SetCursorPosX(220);
            var size = new Vector2(150, ImGui.GetFrameHeight());
            
            Type enumType = typeof(T);
            var values = Enum.GetValues(enumType);

            var valueNames = new string[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                var v = values.GetValue(i);
                valueNames[i] = v != null
                                    ? Enum.GetName(typeof(T), v)
                                    : "?? undefined";
            }

            ImGui.SetNextItemWidth(size.X);
            // FIXME: using only "##dropdown" did not allow for multiple combos (see for example renderSequenceWindow.cs)
            // so we add the type and label here - but this is only a temporary hack...
            var modified = ImGui.Combo($"##dropDown{enumType}{label}", ref index, valueNames, valueNames.Length, valueNames.Length);
            return modified;
        }
    }

    public static class InputWithTypeAheadSearch
    {
        public static bool Draw(string id, ref string text, IOrderedEnumerable<string> items)
        {
            
            if (_isSearchResultWindowOpen)
            {
                if (ImGui.IsKeyPressed((ImGuiKey)Key.CursorDown, true))
                {
                    if (_lastResults.Count > 0)
                    {
                        _selectedResultIndex++;
                        _selectedResultIndex %= _lastResults.Count;
                    }
                }
                else if (ImGui.IsKeyPressed((ImGuiKey)Key.CursorUp, true))
                {
                    if (_lastResults.Count > 0)
                    {
                        _selectedResultIndex--;
                        if (_selectedResultIndex < 0)
                            _selectedResultIndex = _lastResults.Count - 1;
                    }
                }
            }
            
            var wasChanged = ImGui.InputText(id, ref text, 256);

            if (ImGui.IsItemActivated())
            {
                _lastResults.Clear();
                _selectedResultIndex = -1;
                THelpers.DisableImGuiKeyboardNavigation();
            }

            var isItemDeactivated = ImGui.IsItemDeactivated();
            
            // We defer exit to get clicks on opened popup list
            var lostFocus = isItemDeactivated || ImGui.IsKeyDown((ImGuiKey)Key.Esc);
            
            if ( ImGui.IsItemActive() || _isSearchResultWindowOpen)
            {
                _isSearchResultWindowOpen = true;

                ImGui.SetNextWindowPos(new Vector2(ImGui.GetItemRectMin().X, ImGui.GetItemRectMax().Y));
                ImGui.SetNextWindowSize(new Vector2(ImGui.GetItemRectSize().X, 0));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(7, 7));
                if (ImGui.Begin("##typeAheadSearchPopup", ref _isSearchResultWindowOpen,
                                ImGuiWindowFlags.NoTitleBar 
                                | ImGuiWindowFlags.NoMove 
                                | ImGuiWindowFlags.NoResize 
                                | ImGuiWindowFlags.Tooltip 
                                | ImGuiWindowFlags.NoFocusOnAppearing 
                                | ImGuiWindowFlags.ChildWindow
                                ))
                {
                    _lastResults.Clear();
                    int index = 0;
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Color.Gray.Rgba);
                    foreach (var word in items)
                    {
                        if (word != null && word != text && word.Contains(text))
                        {
                            var isSelected = index == _selectedResultIndex;
                            ImGui.Selectable(word, isSelected);
                            
                            if (ImGui.IsItemClicked() || (isSelected && ImGui.IsKeyPressed((ImGuiKey)Key.Return)))
                            {
                                text = word;
                                wasChanged = true;
                                _isSearchResultWindowOpen = false;
                            }

                            _lastResults.Add(word);
                            if (++index > 30)
                                break;
                        }
                    }
                    ImGui.PopStyleColor();
                }

                ImGui.End();
                ImGui.PopStyleVar();
            }

            if (lostFocus)
            {
                THelpers.RestoreImGuiKeyboardNavigation();
                _isSearchResultWindowOpen = false;
            }

            return wasChanged;
        }

        private static List<string> _lastResults = new List<string>();
        private static int _selectedResultIndex = 0;
        private static bool _isSearchResultWindowOpen;        
    }
}