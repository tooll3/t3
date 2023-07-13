using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.IO;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Styling
{
    public static class InputWithTypeAheadSearch
    {
        public static bool Draw(string id, ref string text, IOrderedEnumerable<string> items)
        {
            var inputId = ImGui.GetID(id);
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
            
            var wasChanged = ImGui.InputText(id, ref text, 256);

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
                ImGui.SetNextWindowSize(new Vector2(ImGui.GetItemRectSize().X, 0));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(7, 7));
                if (ImGui.Begin("##typeAheadSearchPopup", ref isSearchResultWindowOpen,
                                ImGuiWindowFlags.NoTitleBar 
                                | ImGuiWindowFlags.NoMove 
                                | ImGuiWindowFlags.NoResize 
                                | ImGuiWindowFlags.Tooltip 
                                | ImGuiWindowFlags.NoFocusOnAppearing 
                                | ImGuiWindowFlags.ChildWindow
                               ))
                {
                    _lastTypeAheadResults.Clear();
                    int index = 0;
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, UiColors.Gray.Rgba);
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
                                _activeInputId = 0;
                                //isSearchResultWindowOpen = false;
                            }

                            _lastTypeAheadResults.Add(word);
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
                _activeInputId = 0;
                //isSearchResultWindowOpen = false;
            }

            return wasChanged;
        }

        private static readonly List<string> _lastTypeAheadResults = new();
        private static int _selectedResultIndex = 0;
        private static uint _activeInputId;
        //private static bool _isSearchResultWindowOpen;
    }
}