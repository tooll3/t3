using T3.Editor.Gui.Graph.Dialogs;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph;

internal sealed partial class GraphWindow
{
    private void RefreshRootInstance()
    {
        if (!Package.TryGetRootInstance(out var rootInstance))
        {
            throw new Exception("Could not get root instance from package");
        }

        var previousRoot = RootInstance.Instance;
        
        if (rootInstance == previousRoot)
            return;

        var rootIsComposition = _composition?.Instance == previousRoot;
        RootInstance.Dispose();

        if (rootIsComposition)
        {
            _composition.Dispose();
            _composition = Composition.GetFor(rootInstance, true);
        }
        
        RootInstance = Composition.GetFor(rootInstance, true);
    }
    internal bool TrySetCompositionOp(IReadOnlyList<Guid> path, ICanvas.Transition transition = ICanvas.Transition.Undefined, Guid? nextSelectedUi = null)
    {
        if (_compositionForDisposal != null)
            throw new Exception($"New composition was requested before the previous one was disposed");

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
        _compositionForDisposal = previousComposition;

        UserSettings.SaveLastViewedOpForWindow(this, _composition.SymbolChildId);
        return true;
    }

    void DisposeOfCompositions()
    {
        for (int i = _compositionsWaitingForDisposal.Count - 1; i >= 0; i--)
        {
            var composition = _compositionsWaitingForDisposal[i];

            var symbolChildId = composition.SymbolChildId;
            if (!_compositionPath.Contains(symbolChildId))
            {
                composition.Dispose();
                _compositionsWaitingForDisposal.RemoveAt(i);
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

    private void OnDuplicationComplete()
    {
        if(_compositionForDisposal == null)
            throw new InvalidOperationException("No duplication composition was set");
        
        _compositionForDisposal.MarkDuplicationComplete();
        _compositionForDisposal.Dispose();
        _compositionForDisposal = null;
    }

    private readonly DuplicateSymbolDialog _duplicateSymbolDialog = new();
    private string _dupeReadonlyNamespace = "";
    private string _dupeReadonlyName = "";
    private string _dupeReadonlyDescription = "";
    private Composition? _compositionForDisposal;
    private readonly List<Guid> _compositionPath = [];

    internal Composition RootInstance { get; private set; }

    private bool _initializedAfterLayoutReady;
    private readonly List<Composition> _compositionsWaitingForDisposal = new();
    public readonly Structure Structure;
}