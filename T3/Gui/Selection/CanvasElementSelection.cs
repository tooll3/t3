using System.Collections.Generic;
using T3.Core.Logging;

namespace T3.Gui.Selection
{
    public class CanvasElementSelection
    {
        public void Clear()
        {
            Selection.Clear();
        }


        public  void SetSelection(ISelectableCanvasObject node)
        {
            if (node is SymbolChildUi)
            {
                Log.Warning("Setting selection to a SymbolChildUi without providing instance will lead to problems.");
            }

            Clear();
            AddSelection(node);
        }

        public  void AddSelection(ISelectableCanvasObject node)
        {
            if (Selection.Contains(node))
                return;

            Selection.Add(node);
        }
        
        public  IEnumerable<T> GetSelectedNodes<T>() where T : ISelectableCanvasObject
        {
            foreach (var item in Selection)
            {
                if (item is T typedItem)
                    yield return typedItem;
            }
        }
        
        public  bool IsNodeSelected(ISelectableCanvasObject node)
        {
            return Selection.Contains(node);
        }

        public  void DeselectNode(ISelectableCanvasObject node)
        {
            Selection.Remove(node);
        }

        
        public  bool IsAnythingSelected()
        {
            return Selection.Count > 0;
        }
        
        public  readonly List<ISelectableCanvasObject> Selection = new();
    }
}