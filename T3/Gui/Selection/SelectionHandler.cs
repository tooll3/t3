using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace T3.Gui.Selection
{
    /**
     * Handles the selection to Controls that implement ISelectable 
     * (OperatorWidgets, ConnectLines, CurvePoints, etc)
     */
    public class SelectionHandler
    {
        public class FirstSelectedChangedEventArgs : EventArgs
        {
            public FirstSelectedChangedEventArgs(ISelectableNode element)
            {
                Element = element;
            }

            public ISelectableNode Element { get; private set; }
        }

        public class SelectionChangedEventArgs : EventArgs
        {
            public SelectionChangedEventArgs(List<ISelectableNode> elements)
            {
                SelectedElements = elements;
            }

            public List<ISelectableNode> SelectedElements { get; private set; }
        }

        public bool Enabled { get; set; }
        public event EventHandler<FirstSelectedChangedEventArgs> FirstSelectedChanged = (o, a) => { };
        public event EventHandler<SelectionChangedEventArgs> SelectionChanged = (o, a) => { };

        public List<ISelectableNode> SelectedElements { get; private set; } // Question: Should we refactor this to use ObservableCollection()?

        public SelectionHandler()
        {
            SelectedElements = new List<ISelectableNode>();
            Enabled = true;
        }

        public IEnumerable<T> GetSelectedElementsOfType<T>() where T : class
        {
            return from element in SelectedElements
                   let e = element as T
                   where e != null
                   select e;
        }

        public T FirstSelectedElementsOfType<T>() where T : class
        {
            return GetSelectedElementsOfType<T>().FirstOrDefault();
        }

        public void AddElement(ISelectableNode e)
        {
            if (!Enabled || SelectedElements.Contains(e))
                return;

            e.IsSelected = true;

            SelectedElements.Add(e);
            TriggerSelectionChangedEvent();

            if (SelectedElements.Count == 1)
            {
                var watch = new Stopwatch();
                watch.Start();
                FirstSelectedChanged(this, new FirstSelectedChangedEventArgs(e));
                watch.Stop();
            }
        }

        public void AddElements(List<ISelectableNode> elements)
        {
            if (!Enabled)
                return;
            bool elementChanged = false;
            int originalCount = SelectedElements.Count;

            elements.ForEach(e =>
                             {
                                 if (!SelectedElements.Contains(e))
                                 {
                                     e.IsSelected = true;
                                     SelectedElements.Add(e);
                                     elementChanged = true;
                                 }
                             });
            if (elementChanged)
            {
                if (originalCount == 0)
                {
                    var watch = new Stopwatch();
                    watch.Start();
                    FirstSelectedChanged(this, new FirstSelectedChangedEventArgs(SelectedElements.FirstOrDefault()));
                    watch.Stop();
                }

                TriggerSelectionChangedEvent();
            }
        }

        public void SetElement(ISelectableNode e)
        {
            if (!Enabled || SelectedElements.Contains(e) && SelectedElements.Count == 1)
                return;

            SelectedElements.ForEach(el => el.IsSelected = false);
            SelectedElements.Clear();
            AddElement(e); // eventually triggers SelectionChangedEvent;
        }

        public void SetElements(List<ISelectableNode> newSelection)
        {
            if (!Enabled)
                return;

            ISelectableNode originalFirst = null;
            if (SelectedElements.Count > 0)
            {
                originalFirst = SelectedElements[0];
            }

            if (newSelection.Count == 0)
            {
                Clear();
                return;
            }

            List<ISelectableNode> elementsToUnselect = new List<ISelectableNode>();
            List<ISelectableNode> elementsToKeep = new List<ISelectableNode>();

            foreach (var alreadySelected in SelectedElements)
            {
                if (newSelection.Contains(alreadySelected))
                {
                    elementsToKeep.Add(alreadySelected);
                }
                else
                {
                    elementsToUnselect.Add(alreadySelected);
                }
            }

            bool nothingNewSelected = true;
            foreach (var e in newSelection)
            {
                if (!elementsToKeep.Contains(e))
                {
                    e.IsSelected = true;
                    elementsToKeep.Add(e);
                    nothingNewSelected = false;
                }
            }

            if (elementsToUnselect.Count == 0 && nothingNewSelected)
                return;

            elementsToUnselect.ForEach(e => e.IsSelected = false);
            SelectedElements = elementsToKeep;

            if (SelectedElements.Count > 0 && SelectedElements[0] != originalFirst)
            {
                var watch = new Stopwatch();
                watch.Start();

                FirstSelectedChanged(this, new FirstSelectedChangedEventArgs(SelectedElements[0]));
                watch.Stop();
            }

            TriggerSelectionChangedEvent();
        }

        public void RemoveElement(ISelectableNode e)
        {
            if (!Enabled)
                return;

            if (SelectedElements.Remove(e))
            {
                e.IsSelected = false;
                FirstSelectedChanged(this, new FirstSelectedChangedEventArgs(SelectedElements.FirstOrDefault()));
                TriggerSelectionChangedEvent();
            }
        }

        public void RemoveElements(List<ISelectableNode> elements)
        {
            if (!Enabled)
                return;

            bool elementChanged = false;
            ISelectableNode firstElement = SelectedElements.FirstOrDefault();

            foreach (var e in elements)
            {
                if (SelectedElements.Remove(e))
                {
                    e.IsSelected = false;
                    elementChanged = true;
                }
            }

            if (elementChanged)
            {
                if (firstElement != SelectedElements.FirstOrDefault())
                    FirstSelectedChanged(this, new FirstSelectedChangedEventArgs(SelectedElements.FirstOrDefault()));
                TriggerSelectionChangedEvent();
            }
        }

        public void ToggleElement(ISelectableNode e)
        {
            if (!Enabled)
                return;

            if (e.IsSelected)
                RemoveElement(e);
            else
                AddElement(e);
        }

        public void ToggleElements(List<ISelectableNode> elements)
        {
            if (!Enabled)
                return;
            elements.ForEach(ToggleElement);
        }

        public void Clear()
        {
            if (!Enabled || SelectedElements.Count == 0)
                return;
            SelectedElements.ForEach(e => e.IsSelected = false);
            SelectedElements.Clear();
            FirstSelectedChanged(this, new FirstSelectedChangedEventArgs(null));
            TriggerSelectionChangedEvent();
        }

        #region helper functions
        /**
         * This handler can be used to refresh UI if selection changes
         * E.g. udpate CurveLine highlights 
         */
        private void TriggerSelectionChangedEvent()
        {
            if (SelectionChanged != null)
            {
                SelectionChanged(this, new SelectionChangedEventArgs(SelectedElements));
            }
        }
        #endregion
    }
}