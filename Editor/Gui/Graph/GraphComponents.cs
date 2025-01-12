#nullable enable
using T3.Core.Operator;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Dialogs;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.TimeLine;

namespace T3.Editor.UiModel;

internal sealed class GraphComponents
{
    public readonly NavigationHistory NavigationHistory;
    public readonly NodeSelection NodeSelection;
    public readonly GraphImageBackground GraphImageBackground;
    public readonly NodeNavigation NodeNavigation;
    public Structure Structure => OpenedProject.Structure;

    private IGraphCanvas _graphCanvas;
    public IGraphCanvas GraphCanvas => _graphCanvas;
    
    private readonly Stack<Composition> _compositionsForDisposal = new();
    public OpenedProject OpenedProject { get; }
    private readonly List<Guid> _compositionPath = [];
    public Composition? Composition { get; set; }
    public Instance? CompositionOp => Composition?.Instance;

    public readonly TimeLineCanvas TimeLineCanvas;

    public event Action<GraphComponents, Guid> CompositionChanged;

    private GraphComponents(OpenedProject openedProject)
    {
        var structure = openedProject.Structure;
        OpenedProject = openedProject;
        NavigationHistory = new NavigationHistory(structure);
        NodeSelection = new NodeSelection(NavigationHistory, structure);
        GraphImageBackground = new GraphImageBackground(NodeSelection, structure);
        
        var getCompositionOp = () => CompositionOp;
        NodeNavigation = new NodeNavigation(structure, NavigationHistory, getCompositionOp);
        TimeLineCanvas = new TimeLineCanvas(NodeSelection, getCompositionOp, TrySetCompositionOpToChild);
        
        _duplicateSymbolDialog.Closed += DisposeLatestComposition;
    }

    public static GraphComponents FromLegacyGraphCanvas(OpenedProject openedProject)
    {
        var components = new GraphComponents(openedProject);
        components._graphCanvas = new GraphCanvas(components.NodeSelection, components.Structure, components.NavigationHistory, components.NodeNavigation, 
                                                  getComposition: () => components.CompositionOp);
        components._graphCanvas.Components = components;
        return components;
    }

    public void DisposeLatestComposition()
    {
        var composition = _compositionsForDisposal.Pop();
        composition.Dispose();
    }

    public bool TrySetCompositionOp(IReadOnlyList<Guid> path, ICanvas.Transition transition = ICanvas.Transition.Undefined, Guid? nextSelectedUi = null)
    {
        var structure = OpenedProject.Structure;
        var newCompositionInstance = structure.GetInstanceFromIdPath(path);

        if (newCompositionInstance == null)
        {
            var pathString = string.Join('/', structure.GetReadableInstancePath(path));
            Log.Error("Failed to find instance with path " + pathString);
            return false;
        }

        // composition is only null once in the very first call to TrySetCompositionOp
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (Composition != null)
        {
            if (path[0] != OpenedProject.RootInstance.Instance.SymbolChildId)
            {
                throw new Exception("Root instance is not the first element in the path");
            }

            if (Composition.SymbolChildId == newCompositionInstance.SymbolChildId)
            {
                if (nextSelectedUi != null)
                {
                    var instance = Composition.Instance.Children[nextSelectedUi.Value];
                    NodeSelection.SetSelection(instance.GetChildUi()!, instance);
                }

                return true;
            }
        }

        var previousComposition = Composition;
        Composition = Composition.GetFor(newCompositionInstance)!;
        _compositionPath.Clear();
        _compositionPath.AddRange(path);

        TimeLineCanvas.ClearSelection();

        if (nextSelectedUi != null)
        {
            var instance = Composition.Instance.Children[nextSelectedUi.Value];
            var symbolChildUi = instance.GetChildUi();

            if (symbolChildUi != null)
                NodeSelection.SetSelection(symbolChildUi, instance);
            else
                NodeSelection.Clear();
        }
        else
        {
            NodeSelection.Clear();
        }

        GraphCanvas.ApplyComposition(transition, newCompositionInstance.SymbolChildId);
        
        CompositionChanged?.Invoke(this, Composition.SymbolChildId);
        return true;
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
            var symbolChildUi = parent.GetSymbolUi().ChildUis[instance.SymbolChildId];
            _duplicateSymbolDialog.Draw(compositionOp: latestComposition.Instance.Parent,
                                        selectedChildUis: [symbolChildUi],
                                        nameSpace: ref _dupeReadonlyNamespace,
                                        newTypeName: ref _dupeReadonlyName,
                                        description: ref _dupeReadonlyDescription,
                                        isReload: true);
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
}