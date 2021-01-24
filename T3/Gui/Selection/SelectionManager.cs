using System;
using System.Collections.Generic;
using System.Diagnostics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Gui.Graph.Interaction;

namespace T3.Gui.Selection
{
    /// <summary>
    /// Some notes on selection handling:
    /// 
    /// - We can't store references to instances because this leads to null-references and reference leaks. For this
    ///   reason we save id-paths: Basically the nesting path of instances from the dashboard operator down.
    ///
    /// - Frequently we want to select the parent operator when clicking on the background of a composition (e.g. to
    ///   to show it's parameters.  
    /// </summary>
    public static class SelectionManager
    {
        public static void Clear()
        {
            Selection.ForEach(TransformGizmoHandling.RemoveTransformCallback);
            Selection.Clear();
        }

        /// <summary>
        /// This called when clicking on background
        /// </summary>
        public static void SetSelectionToParent(Instance instance)
        {
            Clear();
            _selectedComposition = instance;
        }

        public static void SetSelection(ISelectableNode node)
        {
            if (node is SymbolChildUi)
            {
                Log.Warning("Setting selection to a SymbolChildUi without providing instance will lead to problems.");
            }

            Clear();
            AddSelection(node);
        }

        public static void SetSelection(SymbolChildUi node, Instance instance)
        {
            Clear();
            AddSelection(node, instance);
        }

        public static void AddSelection(ISelectableNode node)
        {
            _selectedComposition = null;
            if (Selection.Contains(node))
                return;

            Selection.Add(node);
        }

        public static void AddSelection(SymbolChildUi node, Instance instance)
        {
            _selectedComposition = null;
            if (Selection.Contains(node))
                return;

            Selection.Add(node);
            if (instance != null)
            {
                ChildUiInstanceIdPaths[node] = NodeOperations.BuildIdPathForInstance(instance);
                if (instance is ITransformable transformable)
                {
                    transformable.TransformCallback = TransformGizmoHandling.TransformCallback;
                    TransformGizmoHandling.RegisteredTransformCallbacks[node] = transformable;
                }
            }
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



        public static Instance GetSelectedInstance()
        {
            if (Selection.Count == 0)
                return _selectedComposition;

            if (Selection[0] is SymbolChildUi firstNode)
            {
                if (!ChildUiInstanceIdPaths.ContainsKey(firstNode))
                {
                    Log.Error("Failed to access id-path of selected childUi " + firstNode.SymbolChild.Name);
                    Clear();
                    return null;
                }

                var idPath = ChildUiInstanceIdPaths[firstNode];
                return NodeOperations.GetInstanceFromIdPath(idPath);
            }

            return null;
        }

        public static Instance GetSelectedComposition()
        {
            return _selectedComposition;
        }

        public static Instance GetCompositionForSelection()
        {
            if (Selection.Count == 0)
                return _selectedComposition;

            if (!(Selection[0] is SymbolChildUi firstNode))
                return null;

            var idPath = ChildUiInstanceIdPaths[firstNode];
            var instanceFromIdPath = NodeOperations.GetInstanceFromIdPath(idPath);
            return instanceFromIdPath?.Parent;
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

        public static void RemoveSelection(ISelectableNode node)
        {
            Selection.Remove(node);
            TransformGizmoHandling.RemoveTransformCallback(node);
        }

        
        public static Instance GetInstanceForSymbolChildUi(SymbolChildUi symbolChildUi)
        {
            var idPath = ChildUiInstanceIdPaths[symbolChildUi];
            return (NodeOperations.GetInstanceFromIdPath(idPath));
        }


        
        
        private static Instance _selectedComposition;
        private static readonly List<ISelectableNode> Selection = new List<ISelectableNode>();
        private static readonly Dictionary<SymbolChildUi, List<Guid>> ChildUiInstanceIdPaths = new Dictionary<SymbolChildUi, List<Guid>>();
    }
}