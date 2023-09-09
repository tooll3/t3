using System.Collections.Generic;
using T3.Editor.Gui.Selection;

namespace T3.Editor.Gui.Windows.ResearchCanvas;

public static class BlockSelection
{
    public static readonly HashSet<ISelectableCanvasObject> SelectedNodes = new();

    public static void SetSelection(ISelectableCanvasObject selectedObject)
    {
        SelectedNodes.Clear();
        SelectedNodes.Add(selectedObject);
    }

    public static bool IsNodeSelected(ISelectableCanvasObject node)
    {
        return SelectedNodes.Contains(node);
    }

    public static void AddSelection(IEnumerable<ISelectableCanvasObject> additionalObjects)
    {
        SelectedNodes.UnionWith(additionalObjects);
    }

    public static void AddSelection(ISelectableCanvasObject additionalObject)
    {
        SelectedNodes.Add(additionalObject);
    }

    public static void DeselectNode(ISelectableCanvasObject objectToRemove)
    {
        SelectedNodes.Remove(objectToRemove);
    }
}