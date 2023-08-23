using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using T3.Core.IO;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Styling
{
    public static class InputWithTypeAheadSearch
    {
        
        
        public static bool Draw(string label, ref string filter, IEnumerable<string> items)
        {
            var inputId = ImGui.GetID(label);
            var isSearchResultWindowOpen = inputId == _activeInputId;
            
            if (isSearchResultWindowOpen)
            {
                if (ImGui.IsKeyPressed((ImGuiKey)Key.CursorDown, true))
                {
                    if (_lastTypeAheadResults.Count > 0)
                    {
                        _selectedResultIndex++;
                        _selectedResultIndex %= _lastTypeAheadResults.Count;
                    }
                }
                else if (ImGui.IsKeyPressed((ImGuiKey)Key.CursorUp, true))
                {
                    if (_lastTypeAheadResults.Count > 0)
                    {
                        _selectedResultIndex--;
                        if (_selectedResultIndex < 0)
                            _selectedResultIndex = _lastTypeAheadResults.Count - 1;
                    }
                }
            }
            
            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.Text.Rgba);
            var wasChanged = ImGui.InputText(label, ref filter, 256);
            ImGui.PopStyleColor();

            if (ImGui.IsItemActivated())
            {
                _lastTypeAheadResults.Clear();
                _selectedResultIndex = -1;
                THelpers.DisableImGuiKeyboardNavigation();
            }

            var isItemDeactivated = ImGui.IsItemDeactivated();
            
            // We defer exit to get clicks on opened popup list
            var lostFocus = isItemDeactivated || ImGui.IsKeyDown((ImGuiKey)Key.Esc);
            
            if ( ImGui.IsItemActive() || isSearchResultWindowOpen)
            {
                _activeInputId = inputId;

                ImGui.SetNextWindowPos(new Vector2(ImGui.GetItemRectMin().X, ImGui.GetItemRectMax().Y));
                ImGui.SetNextWindowSize(new Vector2(ImGui.GetItemRectSize().X, 320));
                
                if (ImGui.Begin("##typeAheadSearchPopup", ref isSearchResultWindowOpen,
                                ImGuiWindowFlags.NoTitleBar 
                                | ImGuiWindowFlags.NoMove 
                                | ImGuiWindowFlags.Popup 
                                | ImGuiWindowFlags.ChildWindow
                               ))
                {
                    _lastTypeAheadResults.Clear();
                    var index = 0;
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, UiColors.Gray.Rgba);

                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !ImGui.IsWindowHovered(ImGuiHoveredFlags.RectOnly))
                    {
                        _activeInputId = 0;
                        ImGui.CloseCurrentPopup();
                    }
                        
                    foreach (var word in items)
                    {
                        if (word == null ||  !word.Contains(filter, StringComparison.InvariantCultureIgnoreCase))
                            continue;
                        
                        var isSelected = index == _selectedResultIndex;
                        ImGui.PushStyleColor(ImGuiCol.Text, UiColors.Text.Rgba);
                        ImGui.Selectable(word, isSelected);
                        ImGui.PopStyleColor();
                            
                        if (ImGui.IsItemClicked() || (isSelected && ImGui.IsKeyPressed((ImGuiKey)Key.Return)))
                        {
                            filter = word;
                            wasChanged = true;
                            _activeInputId = 0;
                        }

                        _lastTypeAheadResults.Add(word);
                        if (++index > 100)
                            break;
                    }
                    ImGui.PopStyleColor();
                }

                ImGui.End();
            }

            if (lostFocus)
            {
                THelpers.RestoreImGuiKeyboardNavigation();
            }

            return wasChanged;
        }

        private static readonly List<string> _lastTypeAheadResults = new();
        private static int _selectedResultIndex;
        private static uint _activeInputId;
    }
}