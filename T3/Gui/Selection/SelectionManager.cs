using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;
using T3.Gui.InputUi;

namespace T3.Gui.Selection
{
    public static class SelectionManager
    {
        public static void Clear()
        {
            Selection.Clear();
        }
        
        public static void SetSelectionToParent(Instance instance)
        {
            Selection.Clear();
            _parent = instance;
        }

        public static void SetSelection(ISelectableNode node)
        {
            Selection.Clear();
            AddSelection(node);
        }

        
        public static void SetSelection(SymbolChildUi node, Instance instance)
        {
            Selection.Clear();
            AddSelection(node, instance);
        }

        public static void AddSelection(ISelectableNode node)
        {
            _parent = null;
            Selection.Add(node);
        }

        public static void AddSelection(SymbolChildUi node, Instance instance)
        {
            _parent = null;
            Selection.Add(node);
            if(instance != null)
                InstancesForChildUis[node] = NodeOperations.BuildIdPathForInstance(instance);
        }

        public static void RemoveSelection(ISelectableNode node)
        {
            Selection.Remove(node);
        }

        
        public static IEnumerable<T> GetSelectedNodes<T>() where T:ISelectableNode
        {
            foreach (var item in Selection)
            {
                if (item is T typedItem)
                    yield return typedItem;
            }
        } 
        
        public static bool IsNodeSelected(ISelectableNode node)
        {
            return Selection.Contains(node);
        }

        public static bool IsAnythingSelected()
        {
            return Selection.Count > 0;
        }

        
        public static void ProcessNewFrame()
        {
            NodesSelectedLastFrame.Clear();
            foreach (var n in Selection)
            {
                if (!LastFrameSelection.Contains(n))
                    NodesSelectedLastFrame.Add(n);
            }

            LastFrameSelection = Selection;
        }

        
        public static Instance GetSelectedInstance()
        {
            if (Selection.Count == 0)
                return _parent;

            if (Selection[0] is SymbolChildUi firstNode)
            {
                var idPath = InstancesForChildUis[firstNode];
                return NodeOperations.GetInstanceFromIdPath(idPath);
            }

            return null;
        }

        private static Instance _parent;
        
        private static readonly List<ISelectableNode> Selection = new List<ISelectableNode>();
        private static readonly List<ISelectableNode> NodesSelectedLastFrame = new List<ISelectableNode>();
        public static List<ISelectableNode> LastFrameSelection = new List<ISelectableNode>();
        
        public static Dictionary<SymbolChildUi, List<Guid>> InstancesForChildUis = new Dictionary<SymbolChildUi, List<Guid>>();
    }
}