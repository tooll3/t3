using T3.Editor.UiModel;

namespace T3.Editor.Gui.Selection
{
    public class CanvasElementSelection : ISelection
    {
        public void Clear()
        {
            SelectedElements.Clear();
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
            if (SelectedElements.Contains(node))
                return;

            SelectedElements.Add(node);
        }
        
        public  IEnumerable<T> GetSelectedNodes<T>() where T : ISelectableCanvasObject
        {
            foreach (var item in SelectedElements)
            {
                if (item is T typedItem)
                    yield return typedItem;
            }
        }
        
        public  bool IsNodeSelected(ISelectableCanvasObject node)
        {
            return SelectedElements.Contains(node);
        }

        public  void DeselectNode(ISelectableCanvasObject node)
        {
            SelectedElements.Remove(node);
        }

        
        public  bool IsAnythingSelected()
        {
            return SelectedElements.Count > 0;
        }
        
        public  readonly List<ISelectableCanvasObject> SelectedElements = new();
    }
}