using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using T3.Core.IO;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Styling;

public static class SearchableDropDown
{

    public enum ItemResults
    {
        FilteredOut,
        Visible,
        Activated,
    }
    //public delegate Func<T, string,  bool>;
        
    /// <summary>
    /// 
    /// </summary>
    private static bool Draw<T>(string label, ref T selectedItem, ref string filter, IEnumerable<T> items, Func<T, bool, string,  ItemResults> filterAndDrawItem) where T: IEquatable<T>
    {
            
            
        var inputId = ImGui.GetID(label);
        var isSearchResultWindowOpen = inputId == _activeInputId;
            
        if (isSearchResultWindowOpen)
        {
            if (ImGui.IsKeyPressed((ImGuiKey)Key.CursorDown, true))
            {
                if (_filtedCount > 0)
                {
                    _selectedResultIndex++;
                    _selectedResultIndex %= _filtedCount;
                }
            }
            else if (ImGui.IsKeyPressed((ImGuiKey)Key.CursorUp, true))
            {
                if (_filtedCount > 0)
                {
                    _selectedResultIndex--;
                    if (_selectedResultIndex < 0)
                        _selectedResultIndex = _filtedCount - 1;
                }
            }
        }
            
        ImGui.PushStyleColor(ImGuiCol.Text, UiColors.Text.Rgba);
        var wasChanged = ImGui.InputText(label, ref filter, 256);
        ImGui.PopStyleColor();

        if (ImGui.IsItemActivated())
        {
            _filtedCount = 0;
            //_lastTypeAheadResults.Clear();
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
                
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(7, 7));
            if (ImGui.Begin("##typeAheadSearchPopup", ref isSearchResultWindowOpen,
                            ImGuiWindowFlags.NoTitleBar 
                            | ImGuiWindowFlags.NoMove 
                            | ImGuiWindowFlags.Popup 
                            | ImGuiWindowFlags.ChildWindow
                           ))
            {
                //_lastTypeAheadResults.Clear();
                var index = 0;
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, UiColors.Gray.Rgba);

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !ImGui.IsWindowHovered(ImGuiHoveredFlags.RectOnly))
                {
                    _activeInputId = 0;
                    ImGui.CloseCurrentPopup();
                }

                foreach (var item in items)
                {
                    var isCurrent = item.Equals(selectedItem) || _selectedResultIndex == index;
                    switch (filterAndDrawItem(item, isCurrent, filter))
                    {
                        case ItemResults.FilteredOut:
                            break;
                            
                        case ItemResults.Visible:
                            index++;
                            break;
                            
                        case ItemResults.Activated:
                            //filter = item;
                            wasChanged = true;
                            _activeInputId = 0;
                            break;
                    }
                    // {
                    //     
                    // }
                    // if (item == null ||  !item.Contains(text, StringComparison.InvariantCultureIgnoreCase))
                    //     continue;
                    //
                    // var isSelected = index == _selectedResultIndex;
                    // ImGui.PushStyleColor(ImGuiCol.Text, UiColors.Text.Rgba);
                    // ImGui.Selectable(item, isSelected);
                    // ImGui.PopStyleColor();
                    //     
                    // if (ImGui.IsItemClicked() || (isSelected && ImGui.IsKeyPressed((ImGuiKey)Key.Return)))
                    // {
                    //     text = item;
                    //     wasChanged = true;
                    //     _activeInputId = 0;
                    // }
                        
                    //_lastTypeAheadResults.Add(item);
                    if (++index > 100)
                        break;
                }

                _filtedCount = index;
                ImGui.PopStyleColor();
            }

            ImGui.End();
            ImGui.PopStyleVar();
        }

        if (lostFocus)
        {
            THelpers.RestoreImGuiKeyboardNavigation();
        }

        return wasChanged;
    }
    private static int _selectedResultIndex;
    private static uint _activeInputId;
    private static int _filtedCount;
}