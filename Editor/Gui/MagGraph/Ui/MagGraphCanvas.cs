#nullable enable
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Utils;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.MagGraph.Interaction;
using T3.Editor.Gui.MagGraph.Model;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.MagGraph.Ui;

/**
 * Draws and handles interaction with graph.
 */
internal sealed partial class MagGraphCanvas : ScalableCanvas
{
    public MagGraphCanvas(MagGraphWindow window, NodeSelection nodeSelection)
    {
        EnableParentZoom = false;
        _window = window;
        _context = new GraphUiContext(nodeSelection, this, _window.CompositionOp);
        _nodeSelection = nodeSelection;
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

    public void Draw()
    {
        IsFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows );
        IsHovered = ImGui.IsWindowHovered();
        
        if (_window.CompositionOp == null)
            return;
        
            
        if (_window.CompositionOp != _context.CompositionOp)
            _context = new GraphUiContext(_nodeSelection, this, _window.CompositionOp);
        
        _visibleCanvasArea = ImRect.RectWithSize(InverseTransformPositionFloat(ImGui.GetWindowPos()),
                                                 InverseTransformDirection(ImGui.GetWindowSize()));
        
        
        
        KeyboardActions.HandleKeyboardActions(_context);
        
        if (FitViewToSelectionHandling.FitViewToSelectionRequested)
            FocusViewToSelection(_context);
        
        _context.EditCommentDialog.Draw(_context.Selector);
        
        // Prepare frame
        //_context.Selector.HoveredIds.Clear();
        _context.Layout.ComputeLayout(_context.CompositionOp);
        _context.ItemMovement.PrepareFrame();

        // Debug UI
        if (ImGui.Button("Center"))
            CenterView();

        ImGui.SameLine(0, 5);
        if (ImGui.Button("Rescan"))
            _context.Layout.ComputeLayout(_context.CompositionOp, forceUpdate: true);

        ImGui.SameLine(0, 5);
        ImGui.Checkbox("Debug", ref _enableDebug);

        UpdateCanvas(out _);
        var drawList = ImGui.GetWindowDrawList();

        if (_context.StateMachine.CurrentState is DefaultState)
        {
            _context.ActiveItem = null;
            _context.ActiveOutputId = Guid.Empty;
        }

        DrawBackgroundGrids(drawList);

        // Selection fence...
        {
            HandleFenceSelection(_context, _selectionFence);
        }


        // Items
        foreach (var item in _context.Layout.Items.Values)
        {
            DrawItem(item, drawList, _context);
        }

        Fonts.FontSmall.Scale = 1;

        
        if (_context.ActiveItem != null)
        {
            if (_context.ActiveItem.Id != _lastHoverId)
            {
                _hoverStartTime = ImGui.GetTime();
                _lastHoverId = _context.ActiveItem.Id;
            }
            
            _context.Selector.HoveredIds.Add(_context.ActiveItem.Id);
        }
        else
        {
            _hoverStartTime = ImGui.GetTime();//float.PositiveInfinity;
            _lastHoverId = Guid.Empty;
        }

        
        // Connections
        foreach (var connection in _context.Layout.MagConnections)
        {
            DrawConnection(connection, drawList, _context);
        }


        // Draw temp connections
        foreach (var t in _context.TempConnections)
        {
            var mousePos = ImGui.GetMousePos();
            var sourcePosOnScreen = Vector2.Zero;
            if (t.SourceItem != null)
            {
                var outputLine = t.SourceItem.OutputLines[0];
                var sourcePos = new Vector2(t.SourceItem.Area.Max.X,
                                            t.SourceItem.Area.Min.Y + MagGraphItem.GridSize.Y * (0.5f + outputLine.VisibleIndex));
                sourcePosOnScreen = TransformPosition(sourcePos);

            }
            
            
            var targetPosOnScreen = mousePos;
            if (t.TargetItem != null)
            {
                targetPosOnScreen = TransformPosition(t.TargetItem.PosOnCanvas);
            }

            else if (_context.Placeholder.PlaceholderItem != null)
            {
                targetPosOnScreen = TransformPosition(_context.Placeholder.PlaceholderItem.PosOnCanvas);
            }
                
            var typeColor = TypeUiRegistry.GetPropertiesForType(t.Type).Color;
            var d = Vector2.Distance(sourcePosOnScreen, targetPosOnScreen) / 2;
            
            drawList.AddBezierCubic(sourcePosOnScreen,
                                                                 sourcePosOnScreen + new Vector2(d, 0),
                                                                 targetPosOnScreen - new Vector2(d, 0),
                                                                 targetPosOnScreen,
                                                                 typeColor.Fade(0.6f),
                                                                 2);
        }
        
        _context.Placeholder.DrawPlaceholder(_context, drawList);

        // Draw animated Snap indicator
        {
            var timeSinceSnap = ImGui.GetTime() - _context.ItemMovement.LastSnapTime;
            var progress = MathUtils.RemapAndClamp((float)timeSinceSnap, 0, 0.4f, 1, 0);
            if (progress < 1)
            {
                drawList.AddCircle(TransformPosition(_context.ItemMovement.LastSnapPositionOnCanvas),
                                   progress * 50,
                                   UiColors.ForegroundFull.Fade(progress * 0.2f));
            }
        }

        InputPicking.DrawHiddenInputSelector(_context);
        
        if (FrameStats.Current.OpenedPopUpName == string.Empty)
            CustomComponents.DrawContextMenuForScrollCanvas(() => ContextMenu.DrawContextMenuContent(_context), ref _contextMenuIsOpen);
        
        

        _context.StateMachine. UpdateAfterDraw(_context);
    }

    private bool _contextMenuIsOpen;

    private void DrawBackgroundGrids(ImDrawListPtr drawList)
    {
        var minSize = MathF.Min(MagGraphItem.GridSize.X, MagGraphItem.GridSize.Y);
        var gridSize = Vector2.One * minSize;
        var maxOpacity = 0.25f;

        var fineGrid = MathUtils.RemapAndClamp(Scale.X, 0.5f, 2f, 0.0f, maxOpacity);
        if (fineGrid > 0.01f)
        {
            var color = UiColors.BackgroundFull.Fade(fineGrid);
            DrawBackgroundGrid(drawList, gridSize, color);
        }

        var roughGrid = MathUtils.RemapAndClamp(Scale.X, 0.1f, 2f, 0.0f, maxOpacity);
        if (roughGrid > 0.01f)
        {
            var color = UiColors.BackgroundFull.Fade(roughGrid);
            DrawBackgroundGrid(drawList, gridSize * 5, color);
        }
    }

    private void DrawBackgroundGrid(ImDrawListPtr drawList, Vector2 gridSize, Color color)
    {
        var window = new ImRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize());

        var topLeftOnCanvas = InverseTransformPositionFloat(ImGui.GetWindowPos());
        var alignedTopLeftCanvas = new Vector2((int)(topLeftOnCanvas.X / gridSize.X) * gridSize.X,
                                               (int)(topLeftOnCanvas.Y / gridSize.Y) * gridSize.Y);

        var topLeftOnScreen = TransformPosition(alignedTopLeftCanvas);
        var screenGridSize = TransformDirection(gridSize);

        var count = new Vector2(window.GetWidth() / screenGridSize.X, window.GetHeight() / screenGridSize.Y);

        for (int ix = 0; ix < 200 && ix <= count.X + 1; ix++)
        {
            var x = (int)(topLeftOnScreen.X + ix * screenGridSize.X);
            drawList.AddRectFilled(new Vector2(x, window.Min.Y),
                                   new Vector2(x + 1, window.Max.Y),
                                   color);
        }

        for (int iy = 0; iy < 200 && iy <= count.Y + 1; iy++)
        {
            var y = (int)(topLeftOnScreen.Y + iy * screenGridSize.Y);
            drawList.AddRectFilled(new Vector2(window.Min.X, y),
                                   new Vector2(window.Max.X, y + 1),
                                   color);
        }

        // Commented out. Sadly drawing a point raster creates too many polys and eventually
        // will case rendering artifacts...
        //
        // for (int ix = 0; ix < 200 && ix <= count.X+1; ix++)
        // {
        //     for (int iy = 0; iy < 200 && iy <= count.Y+1; iy++)
        //     {
        //         var pOnScreen = topLeftOnScreen + new Vector2(ix, iy) * screenGridSize;
        //         drawList.AddRectFilled(pOnScreen, pOnScreen + Vector2.One, color);   
        //     }
        // }
    }

    [Flags]
    private enum Borders
    {
        None = 0,
        Up = 1,
        Right = 2,
        Down = 4,
        Left = 8,
    }

    

    private static readonly ImDrawFlags[] _borderRoundings =
        {
            ImDrawFlags.RoundCornersAll, //        0000      
            ImDrawFlags.RoundCornersBottom, //     0001                 up
            ImDrawFlags.RoundCornersLeft, //       0010           right
            ImDrawFlags.RoundCornersBottomLeft, // 0011           right up
            ImDrawFlags.RoundCornersTop, //        0100      down
            ImDrawFlags.RoundCornersNone, //       0101      down       up
            ImDrawFlags.RoundCornersTopLeft, //    0110      down right  
            ImDrawFlags.RoundCornersNone, //       0111      down right up  

            ImDrawFlags.RoundCornersRight, //      1000 left
            ImDrawFlags.RoundCornersBottomRight, //1001 left            up
            ImDrawFlags.RoundCornersNone, //       1010 left      right
            ImDrawFlags.RoundCornersNone, //       1011 left      right up
            ImDrawFlags.RoundCornersTopRight, //   1100 left down
            ImDrawFlags.RoundCornersNone, //       1101 left down       up
            ImDrawFlags.RoundCornersNone, //       1110 left down right  
            ImDrawFlags.RoundCornersNone, //       1111 left down right up  
        };

    private void ShowAnchorPointDebugs(MagGraphItem.AnchorPoint a, bool isInput = false)
    {
        if (!ShowDebug || ImGui.IsMouseDown(ImGuiMouseButton.Left))
            return;

        ImGui.PushFont(Fonts.FontSmall);
        var typeUiProperties = TypeUiRegistry.GetPropertiesForType(a.ConnectionType);

        ImGui.SetCursorScreenPos(TransformPosition(a.PositionOnCanvas) - Vector2.One * ImGui.GetFrameHeight() / 2);
        ImGui.PushStyleColor(ImGuiCol.Text, typeUiProperties.Color.Rgba);
        ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
        var label = isInput ? "I" : "O";
        ImGui.Button($"{label}##{a.GetHashCode()}");
        ImGui.PopStyleColor(2);
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            //ImGui.SetTooltip("hash:" + oa.ConnectionHash);
            ImGui.Text(isInput ? "Input" : "Output");
            ImGui.Text("" + a.ConnectionType.Name);
            ImGui.Text("" + a.ConnectionHash);
            ImGui.EndTooltip();
        }

        ImGui.PopFont();
    }

    private static float Blink => MathF.Sin((float)ImGui.GetTime() * 10) * 0.5f + 0.5f;

    private void HandleFenceSelection(GraphUiContext context, SelectionFence selectionFence)
    {
        var shouldBeActive =
            ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup)
            && _context.StateMachine.CurrentState is DefaultState
            && _context.StateMachine.CurrentState.Time > 0.1f // Prevent glitches when coming from other states.
            ;
        
        if(!shouldBeActive)
            return;
        
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
                    _context.Selector.AddSelection(item, item.Instance);
                }
                else
                {
                    _context.Selector.AddSelection(item);
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

    private readonly MagGraphWindow _window;

    private readonly SelectionFence _selectionFence = new();
    private Vector2 GridSizeOnScreen => TransformDirection(MagGraphItem.GridSize);
    private float CanvasScale => Scale.X;

    public bool ShowDebug =>  _enableDebug;// || ImGui.GetIO().KeyAlt;

    private Guid _lastHoverId;
    private double _hoverStartTime;
    private float HoverTime => (float)(ImGui.GetTime() - _hoverStartTime);
    private bool _enableDebug;
    private GraphUiContext _context;
    private readonly NodeSelection _nodeSelection;

    public void FocusViewToSelection(GraphUiContext context)
    {
        FitAreaOnCanvas(NodeSelection.GetSelectionBounds(context.Selector, context.CompositionOp));
    }
}