using System.Collections.Generic;
using ImGuiNET;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.UiHelpers;

public class PathTreeDrawer
{
    public void Reset()
    {
        _collapsedIndex = 999;
        _openPath.Clear();
        _path.Clear();
    }

    /// <summary>
    /// Check inserts tree nodes and computes visibility of level
    /// </summary>
    public bool DrawEntry(List<string> path, int maxLevel = 2)
    {
        if (path.Count == 0)
            return false;
            
        var matchPathLevelCount = GetMatchingPathLevelCount(path);

        _path = new List<string>(path);
        if (matchPathLevelCount > _collapsedIndex)
            return false;

        var popLevelsCount =  _openPath.Count - matchPathLevelCount;
        for (var index = 0; index < popLevelsCount; index++)
        {
            ImGui.TreePop();
            _openPath.RemoveAt(_openPath.Count - 1);
        }
            
        ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
        ImGui.PushFont(Fonts.FontSmall);
        var maxPushLevelIndex = maxLevel;
        for (var levelIndex = _openPath.Count; levelIndex < maxPushLevelIndex && levelIndex < path.Count; levelIndex++)
        {
            var label = path[levelIndex];
            var isOpen = ImGui.TreeNodeEx(label,ImGuiTreeNodeFlags.DefaultOpen);
            if (!isOpen)
            {
                _collapsedIndex = levelIndex;
                return false;
            }
                
            _openPath.Add(label);
            _collapsedIndex = 9999;
        }
        ImGui.PopFont();
        ImGui.PopStyleColor();


        return true;
    }

    public void Complete()
    {
        for (int index = 0; index < _openPath.Count; index++)
        {
            ImGui.TreePop();
        }
        _openPath.Clear();
    }

    private int GetMatchingPathLevelCount(List<string> other)
    {
        var count = 0;
        while (_path.Count > count && other.Count > count && _path[count] == other[count])
            count++;

        return count;
    }

    private List<string> _path = new();
    private readonly List<string> _openPath = new();
    private int _collapsedIndex = 2;
    //private int _nestingLevel = 0;
    //public int MaxLevel = 0;
}