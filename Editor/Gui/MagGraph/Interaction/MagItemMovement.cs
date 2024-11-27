#nullable enable

using System.Diagnostics;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.MagGraph.Model;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.MagGraph.Ui;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using Vector2 = System.Numerics.Vector2;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable UseWithExpressionToCopyStruct

namespace T3.Editor.Gui.MagGraph.Interaction;

/// <summary>
/// Provides functions for moving, snapping, connecting, insertion etc. of operators. It controlled by the <see cref="StateMachine"/>
/// </summary>
/// <remarks>
/// Things would be slightly more efficient if this would would use SnapGraphItems. However this would
/// prevent us from reusing fence selection. Enforcing this to be used for dragging inputs and outputs
/// makes this class unnecessarily complex.
/// </remarks>
internal sealed partial class MagItemMovement
{
    internal MagItemMovement(GraphUiContext graphUiContext, MagGraphCanvas magGraphCanvas, MagGraphLayout layout, NodeSelection nodeSelection)
    {
        _canvas = magGraphCanvas;
        _layout = layout;
        _nodeSelection = nodeSelection;
        _context = graphUiContext;
    }

    private readonly GraphUiContext _context;

    internal void PrepareFrame()
    {
        PrepareDragInteraction();
    }

    internal void PrepareDragInteraction()
    {
        UpdateBorderConnections(DraggedItems); // Sadly structure might change during drag...
        UpdateSnappedBorderConnections();
    }

    internal void CompleteDragOperation(GraphUiContext context)
    {
        if (context.MacroCommand != null)
        {
            context.MoveElementsCommand?.StoreCurrentValues();
            UndoRedoStack.Add(context.MacroCommand);
        }

        if (!InputPicking.TryInitializeInputSelectionPicker(context))
            Reset();
    }

    /// <summary>
    /// Reset to avoid accidental dragging of previous elements 
    /// </summary>
    internal void StopDragOperation()
    {
        _lastAppliedOffset = Vector2.Zero;
        SplitInsertionPoints.Clear();
    }

    /// <summary>
    /// This will reset the state including all highlights and indicators 
    /// </summary>
    internal void Reset()
    {
        // TODO: should be done by states...
        _context.PrimaryOutputItem = null;
        _context.DraggedPrimaryOutputType = null;

        DraggedItems.Clear();
        _shakeDetector.ResetShaking();
    }

    internal static void SelectActiveItem(GraphUiContext context)
    {
        var item = context.ActiveItem;
        if (item == null)
            return;

        var append = ImGui.GetIO().KeyShift;

        if (context.Selector.IsNodeSelected(item))
        {
            if (append)
            {
                context.Selector.DeselectNode(item, item.Instance);
            }
        }
        else
        {
            if (append)
            {
                item.AddToSelection(context.Selector);
            }
            else
            {
                item.Select(context.Selector);
            }
        }
    }

    internal void UpdateDragging(GraphUiContext context)
    {
        if (!T3Ui.IsCurrentlySaving && _shakeDetector.TestDragForShake(ImGui.GetMousePos()))
        {
            _shakeDetector.ResetShaking();
            if (HandleShakeDisconnect(context))
            {
                _layout.FlagAsChanged();
                return;
            }
        }

        var snappingChanged = HandleSnappedDragging(context);
        if (!snappingChanged)
            return;

        HandleUnsnapAndCollapse(context);
        _layout.FlagAsChanged();

        if (!_snapping.IsSnapped)
            return;

        if (_snapping.IsInsertion)
        {
            if (TrySplitInsert(context))
                _layout.FlagAsChanged();
        }
        else if (TryCreateNewConnectionFromSnap(context))
        {
            _layout.FlagAsChanged();
        }
    }

    private bool HandleShakeDisconnect(GraphUiContext context)
    {
        //Log.Debug("Shake it!");
        Debug.Assert(context.MacroCommand != null);
        if (_borderConnections.Count == 0)
            return false;

        SelectableNodeMovement.DisconnectDraggedNodes(context.CompositionOp, DraggedItems.Select(i => i.Selectable).ToList());
        
        // foreach (var c in _borderConnections)
        // {
        //     context.MacroCommand.AddAndExecCommand(new DeleteConnectionCommand(context.CompositionOp.Symbol, c.AsSymbolConnection(), 0));
        // }

        _layout.FlagAsChanged();
        return true;
    }

    internal static void UpdateLongPressIndicator(float longTapProgress)
    {
        var dl = ImGui.GetWindowDrawList();
        dl.AddCircle(ImGui.GetMousePos(), 100 * (1 - longTapProgress), Color.White.Fade(MathF.Pow(longTapProgress, 3)));
    }

    /// <summary>
    /// Update dragged items and use anchor definitions to identify and use potential snap targets
    /// </summary>
    private bool HandleSnappedDragging(GraphUiContext context)
    {
        var dl = ImGui.GetWindowDrawList();

        if (!_hasDragged)
        {
            _dragStartPosInOpOnCanvas = context.Canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
            _hasDragged = true;
        }

        var mousePosOnCanvas = context.Canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
        var requestedDeltaOnCanvas = mousePosOnCanvas - _dragStartPosInOpOnCanvas;

        var dragExtend = MagGraphItem.GetItemsBounds(DraggedItems);
        dragExtend.Expand(SnapThreshold * context.Canvas.Scale.X);

        if (_canvas.ShowDebug)
        {
            dl.AddCircle(_canvas.TransformPosition(_dragStartPosInOpOnCanvas), 10, Color.Blue);

            dl.AddLine(_canvas.TransformPosition(_dragStartPosInOpOnCanvas),
                       _canvas.TransformPosition(_dragStartPosInOpOnCanvas + requestedDeltaOnCanvas), Color.Blue);

            dl.AddRect(_canvas.TransformPosition(dragExtend.Min),
                       _canvas.TransformPosition(dragExtend.Max),
                       Color.Green.Fade(0.1f));
        }

        var overlappingItems = new List<MagGraphItem>();
        foreach (var otherItem in _layout.Items.Values)
        {
            if (DraggedItems.Contains(otherItem) || !dragExtend.Overlaps(otherItem.Area))
                continue;

            overlappingItems.Add(otherItem);
        }

        // Move back to non-snapped position
        foreach (var n in DraggedItems)
        {
            n.PosOnCanvas -= _lastAppliedOffset; // Move to position
            n.PosOnCanvas += requestedDeltaOnCanvas; // Move to request position
        }

        _lastAppliedOffset = requestedDeltaOnCanvas;
        _snapping.Reset();

        foreach (var ip in SplitInsertionPoints)
        {
            var insertionAnchorItem = DraggedItems.FirstOrDefault(i => i.Id == ip.InputItemId);
            if (insertionAnchorItem == null)
                continue;

            foreach (var otherItem in overlappingItems)
            {
                _snapping.TestItemsForInsertion(otherItem, insertionAnchorItem, ip);
            }
        }

        foreach (var otherItem in overlappingItems)
        {
            foreach (var draggedItem in DraggedItems)
            {
                _snapping.TestItemsForSnap(otherItem, draggedItem, false, _canvas);
                _snapping.TestItemsForSnap(draggedItem, otherItem, true, _canvas);
            }
        }

        // Highlight best distance
        if (_canvas.ShowDebug && _snapping.BestDistance < 500)
        {
            var p1 = _canvas.TransformPosition(_snapping.OutAnchorPos);
            var p2 = _canvas.TransformPosition(_snapping.InputAnchorPos);
            dl.AddLine(p1, p2, UiColors.ForegroundFull.Fade(0.1f), 6);
        }

        // Snapped
        var snapPositionChanged = false;
        if (_snapping.IsSnapped)
        {
            var bestSnapDelta = !_snapping.Reverse
                                    ? _snapping.OutAnchorPos - _snapping.InputAnchorPos
                                    : _snapping.InputAnchorPos - _snapping.OutAnchorPos;

            dl.AddLine(_canvas.TransformPosition(mousePosOnCanvas),
                       context.Canvas.TransformPosition(mousePosOnCanvas) + _canvas.TransformDirection(bestSnapDelta),
                       Color.White);

            var snapTargetPos = mousePosOnCanvas + bestSnapDelta;
            if (Vector2.Distance(snapTargetPos, LastSnapPositionOnCanvas) > 2) // ugh. Magic number
            {
                snapPositionChanged = true;
                LastSnapTime = ImGui.GetTime();
                LastSnapPositionOnCanvas = snapTargetPos;
            }

            foreach (var n in DraggedItems)
            {
                n.PosOnCanvas += bestSnapDelta;
            }

            _lastAppliedOffset += bestSnapDelta;
        }
        // Unsnapped...
        else
        {
            LastSnapPositionOnCanvas = Vector2.Zero;
            LastSnapTime = double.NegativeInfinity;
        }

        var snappingChanged = _snapping.IsSnapped != _wasSnapped || _snapping.IsSnapped && snapPositionChanged;
        _wasSnapped = _snapping.IsSnapped;
        return snappingChanged;
    }

    public void StartDragOperation(GraphUiContext context)
    {
        var snapGraphItems = DraggedItems.Select(i => i as ISelectableCanvasObject).ToList();

        context.MacroCommand = new MacroCommand("Move nodes");
        context.MoveElementsCommand = new ModifyCanvasElementsCommand(context.CompositionOp.Symbol.Id, snapGraphItems, _nodeSelection);
        context.MacroCommand.AddExecutedCommandForUndo(context.MoveElementsCommand);

        _lastAppliedOffset = Vector2.Zero;
        _hasDragged = false;

        UpdateBorderConnections(DraggedItems);
        UpdateSnappedBorderConnections();
        InitSplitInsertionPoints(DraggedItems);
        InitPrimaryDraggedOutput();

        _unsnappedBorderConnectionsBeforeDrag.Clear();
        foreach (var c in _borderConnections)
        {
            if (!c.IsSnapped)
            {
                _unsnappedBorderConnectionsBeforeDrag.Add(c.ConnectionHash);
            }
        }
    }

    private HashSet<int> _unsnappedBorderConnectionsBeforeDrag = [];

    /// <summary>
    /// Handles op disconnection and collapsed
    /// </summary>
    private void HandleUnsnapAndCollapse(GraphUiContext context)
    {
        Debug.Assert(context.MacroCommand != null);

        var unsnappedConnections = new List<MagGraphConnection>();

        var enableDisconnected = UserSettings.Config.DisconnectOnUnsnap ^ ImGui.GetIO().KeyCtrl;
        if (!enableDisconnected)
            return;

        // Delete unsnapped connections
        foreach (var mc in _layout.MagConnections)
        {
            if (!_snappedBorderConnectionHashes.Contains(mc.ConnectionHash))
                continue;

            if (_unsnappedBorderConnectionsBeforeDrag.Contains(mc.ConnectionHash))
                continue;

            unsnappedConnections.Add(mc);

            var connection = new Symbol.Connection(mc.SourceItem.Id,
                                                   mc.SourceOutput.Id,
                                                   mc.TargetItem.Id,
                                                   mc.TargetItem.InputLines[mc.InputLineIndex].Input.Id);

            context.MacroCommand.AddAndExecCommand(new DeleteConnectionCommand(context.CompositionOp.Symbol, connection, 0));
            mc.IsUnlinked = true;
        }

        if (TryCollapseDragFromVerticalStack(context, unsnappedConnections))
            return;

        TryCollapseDisconnectedInputs(context, unsnappedConnections);
    }

    private bool TryCollapseDragFromVerticalStack(GraphUiContext context, List<MagGraphConnection> unsnappedConnections)
    {
        Debug.Assert(context.MacroCommand != null);

        if (unsnappedConnections.Count == 0)
            return false;

        var list = FindVerticalCollapsableConnectionPairs(unsnappedConnections);

        if (list.Count > 1)
        {
            Log.Debug("Collapsing has too many possibilities");
            return false;
        }

        if (list.Count == 0)
            return false;

        var pair = list[0];

        if (Structure.CheckForCycle(pair.Ca.SourceItem.Instance, pair.Cb.TargetItem.Id))
        {
            Log.Debug("Sorry, this connection would create a cycle. (1)");
            return false;
        }

        var potentialMovers = CollectSnappedItems(pair.Cb.TargetItem);

        // Clarify if the subset of items snapped to lower target op is sufficient to fill most gaps

        // First find movable items and then create command with movements
        var movableItems = MoveToCollapseGaps(pair.Ca, pair.Cb, potentialMovers, true);
        if (movableItems.Count == 0)
            return false;

        var affectedItemsAsNodes = movableItems.Select(i => i as ISelectableCanvasObject).ToList();
        var newMoveCommand = new ModifyCanvasElementsCommand(context.CompositionOp.Symbol.Id, affectedItemsAsNodes, _nodeSelection);
        context.MacroCommand.AddExecutedCommandForUndo(newMoveCommand);

        MoveToCollapseGaps(pair.Ca, pair.Cb, movableItems, false);
        newMoveCommand.StoreCurrentValues();

        context.MacroCommand.AddAndExecCommand(new AddConnectionCommand(context.CompositionOp.Symbol,
                                                                        new Symbol.Connection(pair.Ca.SourceItem.Id,
                                                                                              pair.Ca.SourceOutput.Id,
                                                                                              pair.Cb.TargetItem.Id,
                                                                                              pair.Cb.TargetInput.Id),
                                                                        0));
        return true;
    }

    internal static List<SnapCollapseConnectionPair> FindLinkedVerticalCollapsableConnectionPairs(List<MagGraphConnection> unsnappedConnections)
    {
        // Find collapses
        var pairs = new List<SnapCollapseConnectionPair>();

        var ordered = unsnappedConnections.OrderBy(x => x.SourcePos.Y);

        var capturedConnections = new HashSet<MagGraphConnection>();
        // Flood fill vertical snap pairs
        foreach (var topC in ordered)
        {
            if (topC.Style != MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical)
                continue;

            if (capturedConnections.Contains(topC))
                continue;
            
            var linkedOutConnection = topC;
            
            // Find matching linked connections
            while (true)
            {
                if (!linkedOutConnection.IsSnapped)
                    break;
                
                var targetItem = linkedOutConnection.TargetItem;
                if (targetItem == null)
                    break;

                if (!targetItem.TryGetPrimaryOutConnections(out var outConnections))
                    break;

                var cOut = outConnections[0];

                if (!unsnappedConnections.Contains(cOut))
                    break;
                
                linkedOutConnection = cOut;
                
                capturedConnections.Add(linkedOutConnection);
            }

            if (linkedOutConnection != topC)
            {
                pairs.Add(new SnapCollapseConnectionPair(topC, linkedOutConnection));
            }
        }
        return pairs;
    }
    
    
    internal static List<SnapCollapseConnectionPair> FindVerticalCollapsableConnectionPairs(List<MagGraphConnection> unsnappedConnections)
    {

        var list = new List<SnapCollapseConnectionPair>();
        
        // Collapse ops dragged from vertical stack...
        var potentialLinks = new List<MagGraphConnection>();

        foreach (var mc in unsnappedConnections)
        {
            if (mc.Style == MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical)
            {
                potentialLinks.Clear();
                foreach (var cb in unsnappedConnections)
                {
                    if (mc != cb
                        && cb.Style == MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical
                        && cb.SourcePos.Y > mc.SourcePos.Y
                        && Math.Abs(cb.SourcePos.X - mc.SourcePos.X) < SnapTolerance
                        && cb.Type == mc.Type)
                    {
                        potentialLinks.Add(cb);
                    }
                }
        
                if (potentialLinks.Count == 1)
                {
                    list.Add(new SnapCollapseConnectionPair(mc, potentialLinks[0]));
                }
                else if (potentialLinks.Count > 1)
                {
                    Log.Debug("Collapsing with gaps not supported yet");
                }
            }
        }

        return list;
    }

    private void TryCollapseDisconnectedInputs(GraphUiContext context, List<MagGraphConnection> unsnappedConnections)
    {
        if (unsnappedConnections.Count == 0)
            return;

        var snappedItems = new HashSet<MagGraphItem>();
        foreach (var mc in unsnappedConnections)
        {
            CollectSnappedItems(mc.TargetItem, snappedItems);
        }

        // Collapse lines of no longer visible inputs stack...
        var collapseLines = new HashSet<float>();
        foreach (var mc in unsnappedConnections)
        {
            if (mc.Style == MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal
                && mc.InputLineIndex > 0
                && mc.TargetItem.InputLines[mc.InputLineIndex].InputUi.Relevancy == Relevancy.Optional
               )
            {
                collapseLines.Add(mc.SourcePos.Y);
            }
        }

        if (collapseLines.Count == 0)
            return;

        foreach (var y in collapseLines.OrderDescending())
        {
            MoveSnappedItemsVertically(context,
                                       snappedItems,
                                       y,
                                       -MagGraphItem.GridSize.Y
                                      );
        }
    }

    ///<summary>
    /// Iterate through gap lines and move items below upwards
    /// </summary>
    public static HashSet<MagGraphItem> MoveToCollapseGaps(MagGraphConnection ca, MagGraphConnection cb, HashSet<MagGraphItem> movableItems, bool dryRun)
    {
        var minY = ca.SourcePos.Y;
        var maxY = cb.TargetPos.Y;
        var lineWidth = MagGraphItem.GridSize.Y;

        var snapCount = (maxY - minY) / lineWidth;
        var roundedSnapCount = (int)(snapCount + 0.5f);
        var affectedItems = new HashSet<MagGraphItem>();
        for (var lineIndex = roundedSnapCount - 1; lineIndex >= 0; lineIndex--)
        {
            var isLineBlocked = false;
            var middleSnapLineY = minY + (0.5f + lineIndex) * lineWidth;

            foreach (var item in movableItems)
            {
                if (item.Area.Min.Y >= middleSnapLineY || item.Area.Max.Y <= middleSnapLineY)
                    continue;

                isLineBlocked = true;
                break;
            }

            if (isLineBlocked)
                continue;

            // move lines below up one step
            foreach (var item in movableItems)
            {
                if (item.PosOnCanvas.Y < middleSnapLineY)
                    continue;

                affectedItems.Add(item);

                if (!dryRun)
                    item.PosOnCanvas -= new Vector2(0, lineWidth);
            }
        }

        return affectedItems;
    }

    internal sealed record SnapCollapseConnectionPair(MagGraphConnection Ca, MagGraphConnection Cb);

    ///<summary>
    /// Search for potential new connections through snapping
    /// </summary>
    private bool TryCreateNewConnectionFromSnap(GraphUiContext context)
    {
        if (!_snapping.IsSnapped)
            return false;

        Debug.Assert(context.MacroCommand != null);

        var newConnections = new List<Symbol.Connection>();
        foreach (var draggedItem in DraggedItems)
        {
            foreach (var otherItem in _layout.Items.Values)
            {
                if (DraggedItems.Contains(otherItem))
                    continue;

                GetPotentialConnectionsAfterSnap(ref newConnections, draggedItem, otherItem);
                GetPotentialConnectionsAfterSnap(ref newConnections, otherItem, draggedItem);
            }
        }

        foreach (var newConnection in newConnections)
        {
            if (Structure.CheckForCycle(context.CompositionOp.Symbol, newConnection))
            {
                Log.Debug("Sorry, this connection would create a cycle. (4)");
                continue;
            }

            context.MacroCommand.AddAndExecCommand(new AddConnectionCommand(context.CompositionOp.Symbol, newConnection, 0));
        }

        return newConnections.Count > 0;
    }

    private static void GetPotentialConnectionsAfterSnap(ref List<Symbol.Connection> result, MagGraphItem a, MagGraphItem b)
    {
        MagGraphConnection? inConnection;

        for (var bInputLineIndex = 0; bInputLineIndex < b.InputLines.Length; bInputLineIndex++)
        {
            ref var bInputLine = ref b.InputLines[bInputLineIndex];
            inConnection = bInputLine.ConnectionIn;

            int aOutLineIndex;
            for (aOutLineIndex = 0; aOutLineIndex < a.OutputLines.Length; aOutLineIndex++)
            {
                ref var outputLine = ref a.OutputLines[aOutLineIndex]; // Avoid copying data from array
                if (bInputLine.Type != outputLine.Output.ValueType)
                    continue;

                // vertical
                if (aOutLineIndex == 0 && bInputLineIndex == 0)
                {
                    AddPossibleNewConnections(ref result,
                                              ref outputLine,
                                              ref bInputLine,
                                              new Vector2(a.Area.Min.X + MagGraphItem.WidthHalf, a.Area.Max.Y),
                                              new Vector2(b.Area.Min.X + MagGraphItem.WidthHalf, b.Area.Min.Y));
                }

                // horizontal
                if (outputLine.Output.ValueType == bInputLine.Type)
                {
                    AddPossibleNewConnections(ref result,
                                              ref outputLine,
                                              ref bInputLine,
                                              new Vector2(a.Area.Max.X, a.Area.Min.Y + (0.5f + outputLine.VisibleIndex) * MagGraphItem.LineHeight),
                                              new Vector2(b.Area.Min.X, b.Area.Min.Y + (0.5f + bInputLine.VisibleIndex) * MagGraphItem.LineHeight));
                }
            }
        }

        return;

        void AddPossibleNewConnections(ref List<Symbol.Connection> newConnections,
                                       ref MagGraphItem.OutputLine outputLine,
                                       ref MagGraphItem.InputLine inputLine,
                                       Vector2 outPos,
                                       Vector2 inPos)
        {
            if (Vector2.Distance(outPos, inPos) > SnapTolerance)
                return;

            if (inConnection != null)
                return;

            // Clarify if outConnection should also be empty...
            if (outputLine.ConnectionsOut.Count > 0)
            {
                if (outputLine.ConnectionsOut[0].IsSnapped
                    && (outputLine.ConnectionsOut[0].SourcePos - inPos).Length() < SnapTolerance)
                    return;
            }

            newConnections.Add(new Symbol.Connection(
                                                     a.Id,
                                                     outputLine.Id,
                                                     b.Id,
                                                     inputLine.Id
                                                    ));
        }
    }

    private void UpdateBorderConnections(HashSet<MagGraphItem> draggedItems)
    {
        _borderConnections.Clear();
        // This could be optimized by only looking for dragged item connections
        foreach (var c in _layout.MagConnections)
        {
            var targetDragged = draggedItems.Contains(c.TargetItem);
            var sourceDragged = draggedItems.Contains(c.SourceItem);
            if (targetDragged != sourceDragged)
            {
                _borderConnections.Add(c);
            }
        }
    }

    /// <summary>
    /// This is updated every frame...
    /// </summary>
    private void UpdateSnappedBorderConnections()
    {
        _snappedBorderConnectionHashes.Clear();

        foreach (var c in _borderConnections)
        {
            if (c.IsSnapped)
                _snappedBorderConnectionHashes.Add(c.ConnectionHash);
        }
    }

    /// <summary>
    /// When starting a new drag operation, we try to identify border input anchors of the dragged items,
    /// that can be used to insert them between other snapped items.
    /// </summary>
    private void InitSplitInsertionPoints(HashSet<MagGraphItem> draggedItems)
    {
        SplitInsertionPoints.Clear();

        foreach (var itemA in draggedItems)
        {
            foreach (var inputAnchor in itemA.GetInputAnchors())
            {
                // make sure it's a snapped border connection
                if (inputAnchor.ConnectionHash != MagGraphItem.FreeAnchor
                    && !_snappedBorderConnectionHashes.Contains(inputAnchor.ConnectionHash))
                {
                    continue;
                }

                var inlineItems = new List<SplitInsertionPoint>();
                foreach (var itemB in draggedItems)
                {
                    var xy = inputAnchor.Direction == MagGraphItem.Directions.Horizontal ? 0 : 1;

                    if (Math.Abs(itemA.PosOnCanvas[1 - xy] - itemB.PosOnCanvas[1 - xy]) > SnapTolerance)
                        continue;

                    foreach (var outputAnchor in itemB.GetOutputAnchors())
                    {
                        if (outputAnchor.ConnectionHash != MagGraphItem.FreeAnchor
                            && !_snappedBorderConnectionHashes.Contains(outputAnchor.ConnectionHash))
                        {
                            continue;
                        }

                        if (
                            outputAnchor.Direction != inputAnchor.Direction
                            || inputAnchor.ConnectionType != outputAnchor.ConnectionType)
                        {
                            continue;
                        }

                        inlineItems.Add(new SplitInsertionPoint(itemA.Id,
                                                                inputAnchor.SlotId,
                                                                itemB.Id,
                                                                outputAnchor.SlotId,
                                                                inputAnchor.Direction,
                                                                inputAnchor.ConnectionType,
                                                                outputAnchor.PositionOnCanvas[xy] - inputAnchor.PositionOnCanvas[xy]));
                    }
                }

                // Skip insertion lines with gaps
                if (inlineItems.Count == 1)
                {
                    SplitInsertionPoints.Add(inlineItems[0]);
                }
            }
        }
    }

    ///<summary>
    /// Search for potential new connections through snapping
    /// </summary>
    private bool TrySplitInsert(GraphUiContext context)
    {
        if (!_snapping.IsSnapped || _snapping.BestA == null)
            return false;

        Debug.Assert(context.MacroCommand != null);

        var insertionPoint = _snapping.InsertionPoint;

        // Split connection
        var connection = _snapping.BestA.InputLines[0].ConnectionIn;
        if (connection == null)
        {
            Log.Warning("Missing connection?");
            return true;
        }

        if (insertionPoint == null)
        {
            Log.Warning("Insertion point is undefined?");
            return false;
        }

        if (Structure.CheckForCycle(connection.SourceItem.Instance, insertionPoint.InputItemId))
        {
            Log.Debug("Sorry, this connection would create a cycle. (2)");
            return false;
        }

        if (Structure.CheckForCycle(connection.TargetItem.Instance, insertionPoint.OutputItemId))
        {
            Log.Debug("Sorry, this connection would create a cycle. (3)");
            return false;
        }

        context.MacroCommand.AddAndExecCommand(new DeleteConnectionCommand(context.CompositionOp.Symbol,
                                                                           connection.AsSymbolConnection(),
                                                                           0));
        context.MacroCommand.AddAndExecCommand(new AddConnectionCommand(context.CompositionOp.Symbol,
                                                                        new Symbol.Connection(connection.SourceItem.Id,
                                                                                              connection.SourceOutput.Id,
                                                                                              insertionPoint.InputItemId,
                                                                                              insertionPoint.InputId
                                                                                             ), 0));

        context.MacroCommand.AddAndExecCommand(new AddConnectionCommand(context.CompositionOp.Symbol,
                                                                        new Symbol.Connection(insertionPoint.OutputItemId,
                                                                                              insertionPoint.OutputId,
                                                                                              connection.TargetItem.Id,
                                                                                              connection.TargetInput.Id
                                                                                             ), 0));

        MoveSnappedItemsVertically(context,
                                   CollectSnappedItems(_snapping.BestA),
                                   _snapping.OutAnchorPos.Y - MagGraphItem.GridSize.Y / 2,
                                   insertionPoint.Distance);
        return true;
    }

    /// <summary>
    /// Creates and applies a command to move items vertically
    /// </summary>
    /// <returns>
    /// True if some items where moved
    /// </returns>
    public static void MoveSnappedItemsVertically(GraphUiContext context, HashSet<MagGraphItem> snappedItems, float yThreshold, float yDistance)
    {
        Debug.Assert(context.MacroCommand != null);
        var movableItems = new List<MagGraphItem>();
        foreach (var otherItem in snappedItems)
        {
            if (otherItem.PosOnCanvas.Y > yThreshold)
            {
                movableItems.Add(otherItem);
            }
        }

        if (movableItems.Count == 0)
            return;

        // Move items down...
        var affectedItemsAsNodes = movableItems.Select(i => i as ISelectableCanvasObject).ToList();
        var newMoveComment = new ModifyCanvasElementsCommand(context.CompositionOp.Symbol.Id, affectedItemsAsNodes, context.Selector);
        context.MacroCommand.AddExecutedCommandForUndo(newMoveComment);

        foreach (var item in affectedItemsAsNodes)
        {
            item.PosOnCanvas += new Vector2(0, yDistance);
        }

        newMoveComment.StoreCurrentValues();
    }

    private void InitPrimaryDraggedOutput()
    {
        _context.DraggedPrimaryOutputType = null;
        _context.ItemForInputSelection = null;

        _context.PrimaryOutputItem = FindPrimaryOutputItem();
        if (_context.PrimaryOutputItem == null)
            return;

        _context.DraggedPrimaryOutputType = _context.PrimaryOutputItem.PrimaryType;
    }

    /// <summary>
    /// Snapping an item onto a hidden parameter is a tricky ui problem.
    /// To allow this interaction we add "peek" anchor indicator to the dragged item set.
    /// If this is dropped without snapping onto an operator with hidden parameters of the matching type,
    /// we present cables.gl inspired picker interface.
    ///
    /// Set primary types to indicate targets for dragging
    /// This part is a little fishy. To have "some" solution we try to identify dragged
    /// constellations that has a single free horizontal output anchor on the left column.
    /// </summary>
    private MagGraphItem? FindPrimaryOutputItem()
    {
        if (DraggedItems.Count == 0)
            return null;

        if (DraggedItems.Count == 1)
            return DraggedItems.First();

        var itemsOrderedByX = DraggedItems.OrderByDescending(c => c.PosOnCanvas.X);
        var rightItem = itemsOrderedByX.First();

        // Check if there are multiple items on right edge column...
        var rightColumnItemCount = 0;
        foreach (var i in DraggedItems)
        {
            if (Math.Abs(i.PosOnCanvas.X - rightItem.PosOnCanvas.X) < SnapTolerance)
                rightColumnItemCount++;
        }

        if (rightColumnItemCount > 1)
            return null;

        if (DraggedItems.Count != CollectSnappedItems(rightItem).Count)
            return null;

        return rightItem;
    }

    /// <summary>
    /// Add snapped items to the given set or create new set
    /// </summary>
    public static HashSet<MagGraphItem> CollectSnappedItems(MagGraphItem rootItem, HashSet<MagGraphItem>? set = null)
    {
        set ??= [];

        Collect(rootItem);
        return set;

        void Collect(MagGraphItem item)
        {
            if (!set.Add(item))
                return;

            for (var index = 0; index < item.InputLines.Length; index++)
            {
                var c = item.InputLines[index].ConnectionIn;
                if (c == null)
                    continue;

                if (c.IsSnapped && !c.IsUnlinked)
                    Collect(c.SourceItem);
            }

            for (var index = 0; index < item.OutputLines.Length; index++)
            {
                var connections = item.OutputLines[index].ConnectionsOut;
                foreach (var c in connections)
                {
                    if (c.IsSnapped)
                        Collect(c.TargetItem);
                }
            }
        }
    }

    public static HashSet<MagGraphItem> CollectSnappedItems(IEnumerable<MagGraphItem> rootItems)
    {
        var set = new HashSet<MagGraphItem>();

        foreach (var i in rootItems)
        {
            CollectSnappedItems(i, set);
        }

        return set;
    }

    internal void SetDraggedItemIds(List<Guid> selectedIds)
    {
        DraggedItems.Clear();
        foreach (var id in selectedIds)
        {
            if (_layout.Items.TryGetValue(id, out var i))
            {
                DraggedItems.Add(i);
            }
        }
    }

    internal void SetDraggedItems(List<ISelectableCanvasObject> selection)
    {
        DraggedItems.Clear();
        foreach (var s in selection)
        {
            if (_layout.Items.TryGetValue(s.Id, out var i))
            {
                DraggedItems.Add(i);
            }
        }
    }

    internal void SetDraggedItemIdsToSnappedForItem(MagGraphItem item)
    {
        DraggedItems.Clear();
        CollectSnappedItems(item, DraggedItems);
    }

    internal bool IsItemDragged(MagGraphItem item) => DraggedItems.Contains(item);

    internal double LastSnapTime = double.NegativeInfinity;
    internal Vector2 LastSnapPositionOnCanvas;


    internal readonly List<SplitInsertionPoint> SplitInsertionPoints = [];

    private Vector2 _lastAppliedOffset;
    private const float SnapThreshold = 30;
    private readonly List<MagGraphConnection> _borderConnections = [];
    private bool _wasSnapped;
    private static readonly MagItemMovement.Snapping _snapping = new();

    private static bool _hasDragged;
    private Vector2 _dragStartPosInOpOnCanvas;

    private readonly SelectableNodeMovement.ShakeDetector _shakeDetector = new();

    internal readonly HashSet<MagGraphItem> DraggedItems = [];
    //private static readonly HashSet<Guid> _draggedItemIds = [];

    internal sealed record SplitInsertionPoint(
        Guid InputItemId,
        Guid InputId,
        Guid OutputItemId,
        Guid OutputId,
        MagGraphItem.Directions Direction,
        Type Type,
        float Distance);

    private readonly HashSet<int> _snappedBorderConnectionHashes = [];

    private readonly MagGraphCanvas _canvas;
    private readonly MagGraphLayout _layout;
    private readonly NodeSelection _nodeSelection;
    private const float SnapTolerance = 0.01f;
}