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
            if (instance != null)
                ChildUiInstanceIdPaths[node] = NodeOperations.BuildIdPathForInstance(instance);
        }

        public static void RemoveSelection(ISelectableNode node)
        {
            Selection.Remove(node);
        }

        public static IEnumerable<T> GetSelectedNodes<T>() where T : ISelectableNode
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

        
        /// <summary>
        /// This is called at the beginning of each frame.
        /// 
        /// For some events we have to use a frame delay machanism to ui-elements can
        /// respond to updates in a controlled manner (I.e. when rendering the next frame) 
        /// </summary>
        public static void ProcessNewFrame()
        {
            //NodesSelectedLastFrame.Clear();
            // foreach (var n in Selection)
            // {
            //     if (!_lastFrameSelection.Contains(n))
            //         NodesSelectedLastFrame.Add(n);
            // }
        
            //_lastFrameSelection = Selection;
            FitViewToSelectionRequested = _fitViewToSelectionTriggered;
            _fitViewToSelectionTriggered = false;
        }

        public static Instance GetSelectedInstance()
        {
            if (Selection.Count == 0)
                return _parent;

            if (Selection[0] is SymbolChildUi firstNode)
            {
                var idPath = ChildUiInstanceIdPaths[firstNode];
                return NodeOperations.GetInstanceFromIdPath(idPath);
            }

            return null;
        }

        public static Instance GetCompositionForSelection()
        {
            if (Selection.Count == 0)
                return _parent;

            if (Selection[0] is SymbolChildUi firstNode)
            {
                var idPath = ChildUiInstanceIdPaths[firstNode];
                return NodeOperations.GetInstanceFromIdPath(idPath).Parent;
            }

            return null;
        }
        

        public static IEnumerable<SymbolChildUi> GetSelectedSymbolChildUis()
        {
            var result = new List<SymbolChildUi>();
            foreach (var s in Selection)
            {
                if (!(s is SymbolChildUi symbolChildUi))
                    continue;

                result.Add(symbolChildUi);
            }

            return result;
        }

        public static Instance GetInstanceForSymbolChildUi(SymbolChildUi symbolChildUi)
        {
            var idPath = ChildUiInstanceIdPaths[symbolChildUi];
            return (NodeOperations.GetInstanceFromIdPath(idPath));
        }
        
        public static void FitViewToSelection()
        {
            _fitViewToSelectionTriggered = true;
        }


        public static bool FitViewToSelectionRequested = false;
        private static bool _fitViewToSelectionTriggered = false;
        private static Instance _parent;
        private static readonly List<ISelectableNode> Selection = new List<ISelectableNode>();
        //private static readonly List<ISelectableNode> NodesSelectedLastFrame = new List<ISelectableNode>();
        //private static List<ISelectableNode> _lastFrameSelection = new List<ISelectableNode>();
        private static readonly Dictionary<SymbolChildUi, List<Guid>> ChildUiInstanceIdPaths = new Dictionary<SymbolChildUi, List<Guid>>();
    }
}