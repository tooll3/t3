#nullable enable
using System.Diagnostics;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Core.Utils;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Interaction.TransformGizmos;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Interaction
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
    internal class NodeSelection : ISelection
    {
        public NodeSelection(NavigationHistory history, Structure structure)
        {
            _history = history;
            _structure = structure;
        }

        public readonly HashSet<Guid> HoveredIds = new();
        public readonly HashSet<Guid> PinnedIds = new();

        public void Clear()
        {
            TransformGizmoHandling.ClearSelectedTransformables();
            Selection.Clear();
        }

        /// <summary>
        /// This called when clicking on background
        /// </summary>
        public void SetSelectionToComposition(Instance instance)
        {
            _history.UpdateSelectedInstance(instance);
            Clear();
            _childUiInstanceIdPaths.Clear();
            _selectedComposition = instance.InstancePath;
        }

        /// <summary>
        /// Replaces the current selection with the given node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="instance">required if the object is a <see cref="SymbolUi.Child"/></param>
        public void SetSelection(ISelectableCanvasObject node, Instance? instance = null)
        {
            Clear();
            AddSelection(node, instance);
            
            if(node is SymbolUi.Child)
            {
                Debug.Assert(instance != null);
                _history.UpdateSelectedInstance(instance);
            }
        }

        public void AddSelection(ISelectableCanvasObject node, Instance? instance = null)
        {
            if (Selection.Contains(node))
                return;

            if (node is SymbolUi.Child childUi)
            {
                ArgumentNullException.ThrowIfNull(instance);
                _childUiInstanceIdPaths[childUi] = instance.InstancePath;
                if (instance is ITransformable transformable)
                {
                    TransformGizmoHandling.RegisterSelectedTransformable(childUi, transformable);
                }
            }

            Selection.Add(node);
        }

        public void SelectCompositionChild(Instance compositionOp, Guid id)
        {
            if (!Structure.TryGetUiAndInstanceInComposition(id, compositionOp, out var childUi, out var instance))
                return;

            AddSelection(childUi, instance);
        }

        public IEnumerable<T> GetSelectedNodes<T>() where T : ISelectableCanvasObject
        {
            foreach (var item in Selection)
            {
                if (item is T typedItem)
                    yield return typedItem;
            }
        }
        
        public bool IsNodeSelected(ISelectableCanvasObject node) => Selection.Contains(node);

        public bool IsAnythingSelected() => Selection.Count > 0;

        /// <summary>
        /// Returns null if more than onl
        /// </summary>
        public Instance? GetSelectedInstanceWithoutComposition()
        {
            if (Selection.Count != 1)
                return null;

            var selection = GetFirstSelectedInstance();
            return selection == _structure.GetInstanceFromIdPath(_selectedComposition) ? null : selection;
        }
        
        public Instance? GetFirstSelectedInstance()
        {
            if (Selection.Count == 0)
                return _structure.GetInstanceFromIdPath(_selectedComposition);

            if (Selection[0] is SymbolUi.Child firstNode)
            {
                if (!_childUiInstanceIdPaths.TryGetValue(firstNode, out var idPath))
                {
                    Log.Error("Failed to access id-path of selected childUi " + firstNode.SymbolChild.Name);
                    Clear();
                    return null;
                }

                return _structure.GetInstanceFromIdPath(idPath);
            }

            return null;
        }

        public IEnumerable<SymbolUi.Child> GetSelectedChildUis() => GetSelectedNodes<SymbolUi.Child>();
        public IEnumerable<Instance> GetSelectedInstances()
        {
            return GetSelectedNodes<SymbolUi.Child>()
                  .Where(x => _childUiInstanceIdPaths.ContainsKey(x))
                  .Select(symbolChildUi =>
                          {
                              var idPath = _childUiInstanceIdPaths[symbolChildUi];
                              return _structure.GetInstanceFromIdPath(idPath)!;
                          });
        }

        
        /// <summary>
        /// Returns null if there are other selections
        /// </summary>
        public Instance? GetSelectedComposition() => Selection.Count > 0 ? null : _structure.GetInstanceFromIdPath(_selectedComposition);

        public Guid? GetSelectionSymbolChildId()
        {
            if (Selection.Count == 0)
                return _selectedComposition[^1];

            if (Selection[0] is not SymbolUi.Child firstNode)
                return null;

            return firstNode.SymbolChild.Id;
        }

        public void DeselectCompositionChild(Instance compositionOp, Guid symbolChildId)
        {
            if (!Structure.TryGetUiAndInstanceInComposition(symbolChildId, compositionOp, out var childUi, out var instance))
                return;

            DeselectNode(childUi, instance);
        }

        public void DeselectNode(ISelectableCanvasObject node, Instance? instance = null)
        {
            Selection.Remove(node);
            if (instance is ITransformable transformable)
            {
                TransformGizmoHandling.ClearDeselectedTransformableNode(transformable);
            }
        }

        public Instance? GetInstanceForChildUi(SymbolUi.Child symbolChildUi)
        {
            var idPath = _childUiInstanceIdPaths[symbolChildUi];
            return _structure.GetInstanceFromIdPath(idPath);
        }

        private readonly NavigationHistory _history;
        private readonly Structure _structure;

        public readonly List<ISelectableCanvasObject> Selection = new();
        private IReadOnlyList<Guid> _selectedComposition;
        private readonly Dictionary<SymbolUi.Child, IReadOnlyList<Guid>> _childUiInstanceIdPaths = new();
    }
}