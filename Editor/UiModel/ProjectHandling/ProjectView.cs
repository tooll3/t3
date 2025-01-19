#nullable enable
using System.Diagnostics;
using T3.Core.Operator;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Dialogs;
using T3.Editor.Gui.Graph.Window;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.TimeLine;
using T3.Editor.UiModel.Selection;

namespace T3.Editor.UiModel.ProjectHandling;

internal sealed class ProjectView
{
    public readonly NavigationHistory NavigationHistory;
    public readonly NodeSelection NodeSelection;
    public readonly GraphImageBackground GraphImageBackground;
    public readonly NodeNavigation NodeNavigation;
    public Structure Structure => OpenedProject.Structure;

    public IGraphCanvas GraphCanvas { get; set; } = default!; // TODO: remove set accessibility

    private readonly Stack<Composition> _compositionsForDisposal = new();
    public OpenedProject OpenedProject { get; }
    private readonly List<Guid> _compositionPath = [];
    public Composition? Composition { get; set; }
    public Instance? CompositionInstance => Composition?.Instance;

    public readonly TimeLineCanvas TimeLineCanvas;

    public event Action<ProjectView, Guid>? OnCompositionChanged;

    public ProjectView(OpenedProject openedProject, NavigationHistory navigationHistory, NodeSelection nodeSelection, GraphImageBackground graphImageBackground)
    {
        OpenedProject = openedProject;
        _duplicateSymbolDialog.Closed += DisposeLatestComposition;

        NavigationHistory = navigationHistory;
        NodeSelection = nodeSelection;
        GraphImageBackground = graphImageBackground;

        var getCompositionOp = () => CompositionInstance;
        NodeNavigation = new NodeNavigation(openedProject.Structure, NavigationHistory, getCompositionOp);
        TimeLineCanvas = new TimeLineCanvas(NodeSelection, getCompositionOp, TrySetCompositionOpToChild);
    }

    public static void CreateIndependentComponents(OpenedProject openedProject, out NavigationHistory navigationHistory, out NodeSelection nodeSelection,
                                        out GraphImageBackground graphImageBackground)
    {
        var structure = openedProject.Structure;
        navigationHistory = new NavigationHistory(structure);
        nodeSelection = new NodeSelection(navigationHistory, structure);
        graphImageBackground = new GraphImageBackground(nodeSelection, structure);
    }


    public void DisposeLatestComposition()
    {
        var composition = _compositionsForDisposal.Pop();
        composition.Dispose();
    }

    public bool TrySetCompositionOp(IReadOnlyList<Guid> newIdPath, ICanvas.Transition transition = ICanvas.Transition.Undefined, Guid? alsoSelectChildId = null)
    {
        var structure = OpenedProject.Structure;
        var newCompositionInstance = structure.GetInstanceFromIdPath(newIdPath);

        if (newCompositionInstance == null)
        {
            var pathString = string.Join('/', structure.GetReadableInstancePath(newIdPath));
            Log.Error("Failed to find instance with path " + pathString);
            return false;
        }

        // Save previous view for user
        if (_compositionPath.Count > 0)
        {
            var lastSymbolChildId = _compositionPath[^1];
            UserSettings.Config.OperatorViewSettings[lastSymbolChildId] = GraphCanvas.GetTargetScope();
        }
        
        // Set new Composition if required
        var targetCompositionAlreadyActive = Composition != null 
                                             && Composition.SymbolChildId == newCompositionInstance.SymbolChildId;
        if (targetCompositionAlreadyActive)
        {
            if (newIdPath[0] != OpenedProject.RootInstance.Instance.SymbolChildId)
            {
                throw new Exception("Root instance is not the first element in the path");
            }
        }
        else
        {
            Composition = Composition.GetForInstance(newCompositionInstance);
            OnCompositionChanged?.Invoke(this, Composition.SymbolChildId);
        }
        // Although the composition might already be active, the _compositionPath might not have been initialized yet.
        // TODO: This probably is an indication, that this should be refactored into CompositionOp and avoid holding this twice
        _compositionPath.Clear();
        _compositionPath.AddRange(newIdPath);
        
        Debug.Assert(Composition != null);
        
        // Additionally select a child
        NodeSelection.Clear();
        TimeLineCanvas.ClearSelection();

        if (alsoSelectChildId != null)
        {
            var instance = Composition.Instance.Children[alsoSelectChildId.Value];
            NodeSelection.SetSelection(instance.GetChildUi()!, instance);
        }
        else
        {
            GraphCanvas.RestoreLastSavedUserViewForComposition(transition, newCompositionInstance.SymbolChildId);
        }
        
        return true;
    }

    public bool TrySetCompositionOpToChild(Guid symbolChildId)
    {
        // new list as _compositionPath is mutable
        // var path = new List<Guid>(_compositionPath.Count + 1);
        // path.AddRange(_compositionPath);
        // path.Add(symbolChildId);
        List<Guid> path = [.._compositionPath, symbolChildId];
        
        return TrySetCompositionOp(path, ICanvas.Transition.JumpIn);
    }

    public bool TrySetCompositionOpToParent()
    {
        if (_compositionPath.Count == 1)
            return false;

        var previousComposition = Composition;

        // new list as _compositionPath is mutable
        var path = _compositionPath.GetRange(0, _compositionPath.Count - 1);

        // pass the child UI only in case the previous composition was a cloned instance
        return TrySetCompositionOp(path, ICanvas.Transition.JumpOut, previousComposition!.SymbolChildId);
    }

    public void SetBackgroundOutput(Instance instance)
    {
        GraphImageBackground.OutputInstance = instance;
    }

    public void CheckDisposal()
    {
        if (!_compositionsForDisposal.TryPeek(out var latestComposition)) return;

        if (_compositionPath.Contains(latestComposition.SymbolChildId)) return;

        if (latestComposition.NeedsReload)
        {
            _duplicateSymbolDialog.ShowNextFrame(); // actually shows this frame
            var instance = latestComposition.Instance;
            var parent = instance.Parent;
            var symbolChildUi = parent?.GetSymbolUi().ChildUis[instance.SymbolChildId];
            if (symbolChildUi != null && latestComposition.Instance.Parent != null)
            {
                _duplicateSymbolDialog.Draw(compositionOp: latestComposition.Instance.Parent,
                                            selectedChildUis: [symbolChildUi],
                                            nameSpace: ref _dupeReadonlyNamespace,
                                            newTypeName: ref _dupeReadonlyName,
                                            description: ref _dupeReadonlyDescription,
                                            isReload: true);
            }
        }
        else
        {
            DisposeLatestComposition();
        }
    }

    private readonly DuplicateSymbolDialog _duplicateSymbolDialog = new();
    private string _dupeReadonlyNamespace = "";
    private string _dupeReadonlyName = "";
    private string _dupeReadonlyDescription = "";

    public void Close()
    {
        GraphCanvas.Close();
        if (Focused != this)
            return;
        
        foreach (var graphWindow in GraphWindow.GraphWindowInstances)
        {
            if (graphWindow.ProjectView == this)
                graphWindow.CloseView();
            
            if (graphWindow.Config.Visible && graphWindow.ProjectView != null)
                Focused = graphWindow.ProjectView;
        }

        OpenedProject.UnregisterView(this);
    }
    
    public void TakeFocus()
    {
        Focused = this;
    }
    
    public static ProjectView? Focused
    {
        get => _focused;
        private set
        {
            //TODO: check if we need this
            // if (_focused == value)
            //     return;
            //_focused?.OnFocusLost?.Invoke(_focused, _focused);
            _focused = value;
        }
    }
    private static ProjectView? _focused;

}