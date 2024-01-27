using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using T3.Core.IO;
using T3.Editor.Gui.UiHelpers;
using T3.SystemUi;

namespace T3.Editor.Gui.Styling
{
    /// <summary>
    /// Draws a type ahead input 
    /// </summary>
    /// <remarks>
    /// Sadly, the implementation of this component is a single horrible hack.
    /// It's probably the single most ugly peace of ImGui code in the whole codebase.
    /// also see:
    /// https://github.com/ocornut/imgui/issues/718
    /// https://github.com/ocornut/imgui/issues/3725
    ///
    /// It should work for now, but it's likely to break with future versions of ImGui.
    /// </remarks>
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
                if (ImGui.IsItemFocused() && ImGui.IsKeyPressed((ImGuiKey)Key.Return))
                {
                    wasChanged = true;
                    _activeInputId = 0;
                }
                
                if (ImGui.Begin("##typeAheadSearchPopup", ref isSearchResultWindowOpen,
                                ImGuiWindowFlags.NoTitleBar 
                                | ImGuiWindowFlags.NoMove 
                                | ImGuiWindowFlags.Tooltip // ugly as f**k. Sadly .PopUp will lead to random crashes.
                                | ImGuiWindowFlags.NoFocusOnAppearing 
                                | ImGuiWindowFlags.ChildWindow
                               ))
                {
                    _lastTypeAheadResults.Clear();
                    var index = 0;
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, UiColors.Gray.Rgba);
                        
                    foreach (var word in items)
                    {
                        if (word == null ||  !word.Contains(filter, StringComparison.InvariantCultureIgnoreCase))
                            continue;
                        
                        var isSelected = index == _selectedResultIndex;
                        
                        // We can't use IsItemHovered because we need to use Tooltip hack 
                        ImGui.PushStyleColor(ImGuiCol.Text, UiColors.Text.Rgba);
                        ImGui.Selectable(word, isSelected);
                        ImGui.PopStyleColor();

                        var isItemHovered = new ImRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax()).Contains( ImGui.GetMousePos());
                            
                        if ((ImGui.IsMouseClicked(ImGuiMouseButton.Left) && isItemHovered) 
                            || (isSelected && ImGui.IsKeyPressed((ImGuiKey)Key.Return)))
                        {
                            filter = word;
                            wasChanged = true;
                            _activeInputId = 0;
                        }

                        _lastTypeAheadResults.Add(word);
                        if (++index > 100)
                            break;
                    }

                    var isPopupHovered = new ImRect(ImGui.GetWindowContentRegionMin(), ImGui.GetWindowContentRegionMax()).Contains(ImGui.GetMousePos());
                    
                    if (!isPopupHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        _activeInputId = 0;
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