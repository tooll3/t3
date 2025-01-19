#nullable enable
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.MagGraph.Model;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using T3.Editor.UiModel.Modification;
using T3.Editor.UiModel.ProjectHandling;
using T3.Editor.UiModel.Selection;

namespace T3.Editor.Gui.MagGraph.Ui;

/**
 * Draws and handles interaction with graph.
 */
internal sealed partial class MagGraphCanvas : ScalableCanvas, IGraphCanvas
{
    public static ProjectView CreateWithComponents(OpenedProject openedProject)
    {
        ProjectView.CreateIndependentComponents(openedProject,
                                                out var navigationHistory,
                                                out var nodeSelection,
                                                out var graphImageBackground);

        var projectView = new ProjectView(openedProject, navigationHistory, nodeSelection, graphImageBackground)
                              {
                                  Composition = openedProject.RootInstance
                              };

        if (projectView.CompositionOp == null)
        {
            Log.Error("Can't create graph without defined composition op");
            return projectView; // TODO: handle this properly
        }

        var canvas = new MagGraphCanvas(projectView);
        projectView.OnCompositionChanged += canvas.CompositionChangedHandler;

        projectView.GraphCanvas = canvas;
        return projectView;
    }

    private void CompositionChangedHandler(ProjectView arg1, Guid arg2)
    {
        _context.Layout.FlagAsChanged();
    }

    private readonly ProjectView _projectView;

    #region implement IGraph canvas
    bool IGraphCanvas.Destroyed { get => _destroyed; set => _destroyed = value; }

    private bool _viewChangeRequested;
    private CanvasScope _requestedTargetScope;

    void IGraphCanvas.RestoreLastSavedUserViewForComposition(ICanvas.Transition transition, Guid compositionOpSymbolChildId)
    {
        if (!UserSettings.Config.OperatorViewSettings.TryGetValue(compositionOpSymbolChildId, out var savedCanvasScope))
            return;

        _viewChangeRequested = true;
        _requestedTargetScope = savedCanvasScope;
    }

    void IGraphCanvas.FocusViewToSelection()
    {
        Log.Debug("MagGraphCanvas.FocusViewToSelection() Not implemented yet");
    }

    void IGraphCanvas.OpenAndFocusInstance(IReadOnlyList<Guid> path)
    {
        Log.Debug("MagGraphCanvas.OpenAndFocusInstance() Not implemented yet");
    }

    private Instance _previousInstance;

    void IGraphCanvas.BeginDraw(bool backgroundActive, bool bgHasInteractionFocus)
    {
        //TODO: This should probably be handled by CompositionChangedHandler
        if (_projectView.CompositionOp != null && _projectView.CompositionOp != _previousInstance)
        {
            _previousInstance = _projectView.CompositionOp;
            _context = new GraphUiContext(_projectView, this);
        }
    }

    public bool HasActiveInteraction => _context.StateMachine.CurrentState != GraphStates.Default;

    ProjectView IGraphCanvas.ProjectView { set => throw new NotImplementedException(); }

    void IGraphCanvas.Close()
    {
        _destroyed = true;
        _projectView.OnCompositionChanged -= CompositionChangedHandler;
    }

    void IGraphCanvas.CreatePlaceHolderConnectedToInput(SymbolUi.Child symbolChildUi, Symbol.InputDefinition inputInputDefinition)
    {
        Log.Debug($"{nameof(IGraphCanvas.CreatePlaceHolderConnectedToInput)}() not implemented yet");
    }

    void IGraphCanvas.StartDraggingFromInputSlot(SymbolUi.Child symbolChildUi, Symbol.InputDefinition inputInputDefinition)
    {
        Log.Debug($"{nameof(IGraphCanvas.StartDraggingFromInputSlot)}() not implemented yet");
    }
    #endregion

    public MagGraphCanvas(ProjectView projectView)
    {
        _projectView = projectView;
        EnableParentZoom = false;
        _context = new GraphUiContext(projectView, this);
        _nodeSelection = projectView.NodeSelection;
        _previousInstance = projectView.CompositionOp!;

        InitializeCanvasScope(_context);
    }

    private ImRect _visibleCanvasArea;

    public bool IsRectVisible(ImRect rect)
    {
        return _visibleCanvasArea.Overlaps(rect);
    }

    public bool IsItemVisible(ISelectableCanvasObject item)
    {
        return IsRectVisible(ImRect.RectWithSize(item.PosOnCanvas, item.Size));
    }

    public bool IsFocused { get; private set; }
    public bool IsHovered { get; private set; }

    // private Guid _previousCompositionId;

    /// <summary>
    /// This is an intermediate helper method that should be replaced with a generalized implementation shared by
    /// all graph windows. It's especially unfortunate because it relies on GraphWindow.Focus to exist as open window :(
    ///
    /// It uses changes to context.CompositionOp to refresh the view to either the complete content or to the
    /// view saved in user settings...
    /// </summary>
    private void InitializeCanvasScope(GraphUiContext context)
    {
        // if (context.CompositionOp.SymbolChildId == _previousCompositionId)
        //     return;
        //
        if (ProjectView.Focused?.GraphCanvas == null)
            return;

        // _previousCompositionId = context.CompositionOp.SymbolChildId;

        // Meh: This relies on TargetScope already being set to new composition.
        var newCanvasScope = ProjectView.Focused.GraphCanvas.GetTargetScope();
        if (UserSettings.Config.OperatorViewSettings.TryGetValue(context.CompositionOp.SymbolChildId, out var savedCanvasScope))
        {
            newCanvasScope = savedCanvasScope;
        }

        context.Canvas.SetScopeWithTransition(newCanvasScope.Scale, newCanvasScope.Scroll, ICanvas.Transition.Undefined);
    }

    private void HandleSymbolDropping(GraphUiContext context)
    {
        if (!DragHandling.IsDragging)
            return;

        ImGui.SetCursorPos(Vector2.Zero);
        ImGui.InvisibleButton("## drop", ImGui.GetWindowSize());

        if (!DragHandling.TryGetDataDroppedLastItem(DragHandling.SymbolDraggingId, out var data))
            return;

        if (!Guid.TryParse(data, out var guid))
        {
            Log.Warning("Invalid data format for drop? " + data);
            return;
        }

        if (SymbolUiRegistry.TryGetSymbolUi(guid, out var symbolUi))
        {
            var symbol = symbolUi.Symbol;
            var posOnCanvas = InverseTransformPositionFloat(ImGui.GetMousePos());
            if (!SymbolUiRegistry.TryGetSymbolUi(context.CompositionOp.Symbol.Id, out var compositionOpSymbolUi))
            {
                Log.Warning("Failed to get symbol id for " + context.CompositionOp.SymbolChildId);
                return;
            }

            var childUi = GraphOperations.AddSymbolChild(symbol, compositionOpSymbolUi, posOnCanvas);
            var instance = context.CompositionOp.Children[childUi.Id];
            context.Selector.SetSelection(childUi, instance);
            context.Layout.FlagAsChanged();
        }
        else
        {
            Log.Warning($"Symbol {guid} not found in registry");
        }
    }

    private void HandleFenceSelection(GraphUiContext context, SelectionFence selectionFence)
    {
        var shouldBeActive =
                ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup)
                && (_context.StateMachine.CurrentState == GraphStates.Default
                    || _context.StateMachine.CurrentState == GraphStates.HoldBackground)
                && _context.StateMachine.StateTime > 0.01f // Prevent glitches when coming from other states.
            ;

        if (!shouldBeActive)
        {
            selectionFence.Reset();
            return;
        }

        switch (selectionFence.UpdateAndDraw(out var selectMode))
        {
            case SelectionFence.States.PressedButNotMoved:
                if (selectMode == SelectionFence.SelectModes.Replace)
                    _context.Selector.Clear();
                break;

            case SelectionFence.States.Updated:
                HandleSelectionFenceUpdate(selectionFence.BoundsUnclamped, selectMode);
                break;

            case SelectionFence.States.CompletedAsClick:
                // A hack to prevent clearing selection when opening parameter popup
                if (ImGui.IsPopupOpen("", ImGuiPopupFlags.AnyPopup))
                    break;

                _context.Selector.Clear();
                _context.Selector.SetSelectionToComposition(context.CompositionOp);
                break;
            case SelectionFence.States.Inactive:
                break;
            case SelectionFence.States.CompletedAsArea:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    // TODO: Support non graph items like annotations.
    private void HandleSelectionFenceUpdate(ImRect bounds, SelectionFence.SelectModes selectMode)
    {
        var boundsInCanvas = InverseTransformRect(bounds);
        var itemsInFence = (from child in _context.Layout.Items.Values
                            let rect = new ImRect(child.PosOnCanvas, child.PosOnCanvas + child.Size)
                            where rect.Overlaps(boundsInCanvas)
                            select child).ToList();

        if (selectMode == SelectionFence.SelectModes.Replace)
        {
            _context.Selector.Clear();
        }

        foreach (var item in itemsInFence)
        {
            if (selectMode == SelectionFence.SelectModes.Remove)
            {
                _context.Selector.DeselectNode(item, item.Instance);
            }
            else
            {
                if (item.Variant == MagGraphItem.Variants.Operator)
                {
                    _context.Selector.AddSelection(item.Selectable, item.Instance);
                }
                else
                {
                    _context.Selector.AddSelection(item.Selectable);
                }
            }
        }
    }

    private void CenterView()
    {
        var visibleArea = new ImRect();
        var isFirst = true;

        foreach (var item in _context.Layout.Items.Values)
        {
            if (isFirst)
            {
                visibleArea = item.Area;
                isFirst = false;
                continue;
            }

            visibleArea.Add(item.PosOnCanvas);
        }

        FitAreaOnCanvas(visibleArea);
    }

    private float GetHoverTimeForId(Guid id)
    {
        if (id != _lastHoverId)
            return 0;

        return HoverTime;
    }

    private readonly SelectionFence _selectionFence = new();
    private Vector2 GridSizeOnScreen => TransformDirection(MagGraphItem.GridSize);
    private float CanvasScale => Scale.X;

    public bool ShowDebug => _enableDebug; // || ImGui.GetIO().KeyAlt;

    private Guid _lastHoverId;
    private double _hoverStartTime;
    private float HoverTime => (float)(ImGui.GetTime() - _hoverStartTime);
    private bool _enableDebug;
    private GraphUiContext _context;
    private readonly NodeSelection _nodeSelection;
    private bool _destroyed;

    protected override ScalableCanvas? Parent => null;

    public void FocusViewToSelection(GraphUiContext context)
    {
        FitAreaOnCanvas(NodeSelection.GetSelectionBounds(context.Selector, context.CompositionOp));
    }
}