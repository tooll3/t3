using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Editor.Gui.UiHelpers;

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
        
        public static bool Draw(string label, ref string filter, IEnumerable<string> items, bool outlineOnly=false)
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
            
            //ImGui.PushStyleColor(ImGuiCol.Text, UiColors.Text.Rgba);
            filter ??= string.Empty;
            
            if (outlineOnly)
            {
                ImGui.PushStyleColor(ImGuiCol.FrameBg, Color.Transparent.Rgba);
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, Color.Red.Rgba);
                //ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize,0.1f);
            }
            var wasChanged = ImGui.InputText(label, ref filter, 256);
            
            if (outlineOnly)
            {
                var drawList = ImGui.GetWindowDrawList();
                drawList.AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), UiColors.BackgroundInputField, 5);
                //ImGui.PopStyleVar();
                ImGui.PopStyleColor(2);
            }

            
            //ImGui.PopStyleColor();
            
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

                var lastPosition = new Vector2(ImGui.GetItemRectMin().X, ImGui.GetItemRectMax().Y);
                var size = new Vector2(ImGui.GetItemRectSize().X, 320);
                ImGui.SetNextWindowPos(lastPosition);
                ImGui.SetNextWindowSize(size);
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
                    
                    var matches = new List<string>();
                    var others = new List<string>();
                    foreach (var word in items)
                    {
                        if (word != null && (string.IsNullOrWhiteSpace(filter) || word.Contains(filter, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            matches.Add(word);
                        }
                        else
                        {
                            others.Add(word);
                        }
                    }
                    
                    var listItems = (!string.IsNullOrWhiteSpace(filter) && matches.Count  <=1) ? others : matches;
                    foreach (var word in listItems)
                    {
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