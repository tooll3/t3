using System.Collections.Generic;
using System.Linq;
using T3.Editor.Gui.Selection;

namespace T3.Editor.Gui.Windows.ResearchCanvas;

public static class BlockSelection
{
    private static readonly HashSet<ISelectableCanvasObject> _selectedNodes = new();

    public static IEnumerable<ISelectableCanvasObject> GetSelection()
    {
        return _selectedNodes.ToList();
    }
    
    public static void SetSelection(ISelectableCanvasObject selectedObject)
    {
        _selectedNodes.Clear();
        _selectedNodes.Add(selectedObject);
    }

    public static bool IsNodeSelected(ISelectableCanvasObject node)
    {
        return _selectedNodes.Contains(node);
    }

    public static void AddSelection(IEnumerable<ISelectableCanvasObject> additionalObjects)
    {
        _selectedNodes.UnionWith(additionalObjects);
    }

    public static void AddSelection(ISelectableCanvasObject additionalObject)
    {
        _selectedNodes.Add(additionalObject);
    }

    public static void DeselectNode(ISelectableCanvasObject objectToRemove)
    {
        _selectedNodes.Remove(objectToRemove);
    }
}