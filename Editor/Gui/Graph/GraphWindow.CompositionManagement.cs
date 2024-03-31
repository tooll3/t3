using T3.Core.Operator;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph;

internal sealed partial class GraphWindow
{
    internal bool TrySetCompositionOp(IReadOnlyList<Guid> path, ICanvas.Transition transition = ICanvas.Transition.Undefined, Guid? nextSelectedUi = null)
    {
        if (!Package.IsReadOnly)
        {
            // refresh root in case the project root changed in code?
            if (!Package.TryGetRootInstance(out var rootInstance))
            {
                return false;
            }

            _rootInstance = rootInstance!;
        }

        var newCompositionInstance = Structure.GetInstanceFromIdPath(path);

        if (newCompositionInstance == null)
        {
            var pathString = string.Join('/', Structure.GetReadableInstancePath(path));
            Log.Error("Failed to find instance with path " + pathString);
            return false;
        }

        var previousComposition = _composition;

        // composition is only null once in the very first call to TrySetCompositionOp
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (previousComposition != null)
        {
            if (path[0] != RootInstance.Instance.SymbolChildId)
            {
                throw new Exception("Root instance is not the first element in the path");
            }

            if (previousComposition.SymbolChildId == newCompositionInstance.SymbolChildId)
                return true;
        }

        _composition = Composition.GetFor(newCompositionInstance, true)!;
        _compositionPath.Clear();
        _compositionPath.AddRange(path);
        _timeLineCanvas.ClearSelection();

        if (nextSelectedUi != null)
        {
            var instance = _composition.Instance.Children[nextSelectedUi.Value];
            var symbolChildUi = instance.GetChildUi();

            if (symbolChildUi != null)
                GraphCanvas.NodeSelection.SetSelectionToChildUi(symbolChildUi, instance);
            else
                GraphCanvas.NodeSelection.Clear();
        }
        else
        {
            GraphCanvas.NodeSelection.Clear();
        }

        ApplyComposition(transition, previousComposition);
        DisposeOfCompositions(path, previousComposition, _compositionsWaitingForDisposal);

        UserSettings.SaveLastViewedOpForWindow(this, _composition.SymbolChildId);
        return true;

        static void DisposeOfCompositions(IReadOnlyList<Guid> currentPath, Composition previous, List<Composition> compositionsWaitingForDisposal)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (previous != null && !compositionsWaitingForDisposal.Contains(previous))
            {
                compositionsWaitingForDisposal.Add(previous);
            }

            for (int i = compositionsWaitingForDisposal.Count - 1; i >= 0; i--)
            {
                var composition = compositionsWaitingForDisposal[i];
                var symbolChildId = composition.SymbolChildId;
                if (!currentPath.Contains(symbolChildId))
                {
                    composition.Dispose();
                    compositionsWaitingForDisposal.RemoveAt(i);
                }
            }
        }
    }

    public bool TrySetCompositionOpToChild(Guid symbolChildId)
    {
        // new list as _compositionPath is mutable
        var newPathList = new List<Guid>(_compositionPath.Count + 1);
        newPathList.AddRange(_compositionPath);
        newPathList.Add(symbolChildId);
        return TrySetCompositionOp(newPathList, ICanvas.Transition.JumpIn);
    }

    public bool TrySetCompositionOpToParent()
    {
        if (_compositionPath.Count == 1)
            return false;

        var previousComposition = _composition;

        // new list as _compositionPath is mutable
        var path = _compositionPath.GetRange(0, _compositionPath.Count - 1);

        // pass the child UI only in case the previous composition was a cloned instance
        return TrySetCompositionOp(path, ICanvas.Transition.JumpOut, previousComposition.SymbolChildId);
    }

    private void ApplyComposition(ICanvas.Transition transition, Composition previousComposition)
    {
        GraphCanvas.SelectableNodeMovement.Reset();

        var compositionOp = CompositionOp;

        if (previousComposition != null)
        {
            // zoom timeline out if necessary
            if (transition == ICanvas.Transition.JumpOut)
            {
                _timeLineCanvas.UpdateScaleAndTranslation(previousComposition.Instance, transition);
            }

            var targetScope = GraphCanvas.GetTargetScope();
            UserSettings.Config.OperatorViewSettings[previousComposition.SymbolChildId] = targetScope;
        }

        _timeLineCanvas.ClearSelection();
        GraphCanvas.FocusViewToSelection();

        var newProps = GraphCanvas.GetTargetScope();
        if (UserSettings.Config.OperatorViewSettings.TryGetValue(compositionOp.SymbolChildId, out var viewSetting))
        {
            newProps = viewSetting;
        }

        GraphCanvas.SetScopeWithTransition(newProps.Scale, newProps.Scroll, transition);

        if (transition == ICanvas.Transition.JumpIn)
        {
            _timeLineCanvas.UpdateScaleAndTranslation(compositionOp, transition);
        }
    }

    private readonly List<Guid> _compositionPath = [];
    private Instance _rootInstance;
    internal Composition RootInstance => Composition.GetFor(_rootInstance, true);
    private bool _initializedAfterLayoutReady;
    private readonly List<Composition> _compositionsWaitingForDisposal = new();
    public readonly Structure Structure;
}