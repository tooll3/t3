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
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using Vector2 = System.Numerics.Vector2;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable UseWithExpressionToCopyStruct

namespace T3.Editor.Gui.MagGraph.Ui.Interaction;

/// <summary>
/// 
/// </summary>
/// <remarks>
/// Things would be slightly more efficient if this would would use SnapGraphItems. However this would
/// prevent us from reusing fence selection. Enforcing this to be used for dragging inputs and outputs
/// makes this class unnecessarily complex.
/// </remarks>
internal sealed partial class MagItemMovement
{
    public MagItemMovement(GraphUiContext graphUiContext, MagGraphCanvas magGraphCanvas, MagGraphLayout layout, NodeSelection nodeSelection)
    {
        _canvas = magGraphCanvas;
        _layout = layout;
        _nodeSelection = nodeSelection;
        _context = graphUiContext;
    }

    private GraphUiContext _context;


    public void PrepareFrame()
    {
        PrepareItemReferences();
        PrepareDragInteraction();
    }



    private void PrepareDragInteraction()
    {
        UpdateBorderConnections(_draggedItems); // Sadly structure might change during drag...
        UpdateSnappedBorderConnections();
    }

    /// <summary>
    /// For certain edge cases like the release handling of nodes cannot be detected.
    /// This is a work around to clear the state on mouse release
    /// </summary>
    // Todo: Call this from the main loop?
    public void CompleteFrame()
    {
        if (ImGui.IsMouseReleased(0) && _isInProgress)
        {
            StopDragOperation();
        }
    }
    
    /// <summary>
    /// We can't assume the the layout model structure to be stable across frames.
    /// We use Guids to carry over the information for dragged items.
    /// </summary>
    ///
    /// <remarks>
    /// This can eventually be optimized by having a structure counter or a hasChanged flag in layout.
    /// </remarks>
    private void PrepareItemReferences()
    {
        _draggedItems.Clear();
        foreach (var id in _draggedItemIds)
        {
            if (!_layout.Items.TryGetValue(id, out var item))
            {
                Log.Warning("Dragged item no longer valid?");
                continue;
            }

            if (id == PrimaryOutputItemId)
                PrimaryOutputItem = item;

            _draggedItems.Add(item);
        }
    }

    /// <summary>
    /// This is called for ALL movable elements (ops, inputs, outputs) and directly after ImGui.Item
    /// </summary>
    public void HandleForItem(MagGraphItem item, MagGraphCanvas canvas)
    {
        var composition = _context.CompositionOp;
        if (composition == null)
            return;
        
        var isActiveNode = item.Id == _draggedNodeId;
        var isItemHovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup 
                                                | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
        var clickedDown = isItemHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left);

        if(isItemHovered)
            _context.LastHoveredItem = item;
        
        // Start dragging
        if (clickedDown)
        {
            _longTapItemId = item.Id;
            _draggedNodeId = item.Id;
            if (item.IsSelected(_nodeSelection))
            {
                _draggedItemIds.Clear();
                foreach (var s in _nodeSelection.Selection)
                {
                    if (_layout.Items.TryGetValue(s.Id, out var i))
                    {
                        _draggedItemIds.Add(i.Id);
                    }
                }
                PrepareDragInteraction();
            }
            else
            {
                _draggedItems.Clear();
                CollectSnappedItems(item, _draggedItems);

                _draggedItemIds.Clear();
                foreach (var item1 in _draggedItems)
                {
                    _draggedItemIds.Add(item1.Id);
                }
            }

            _mousePressedTime = ImGui.GetTime();

            StartDragOperation(composition);
        }
        // Update dragging...
        else if (isActiveNode && ImGui.IsMouseDown(ImGuiMouseButton.Left) && _isInProgress)
        {
            // Now updated in state machine...
            //UpdateDragging(canvas, composition);
        }
        // Release and complete dragging
        else if (isActiveNode && ImGui.IsMouseReleased(0) && _isInProgress)
        {
            if (_draggedNodeId != item.Id)
                return;

            // Only clicked
            if (!_hasDragged)
            {
                if (!_nodeSelection.IsNodeSelected(item))
                {
                    var replaceSelection = !ImGui.GetIO().KeyShift;
                    if (replaceSelection)
                    {
                        item.Select(_nodeSelection);
                    }
                    else
                    {
                        item.AddToSelection(_nodeSelection);
                    }
                }
                else
                {
                    if (ImGui.GetIO().KeyShift)
                    {
                        _nodeSelection.DeselectNode(item, item.Instance);
                    }
                }
            }
            // Complete drag interactions...
            else
            {
                GraphUiContext._moveElementsCommand?.StoreCurrentValues();
                UndoRedoStack.Add(GraphUiContext.MacroCommand);
                if (!TryInitializeInputSelectionPicker())
                    Reset();
            }

            StopDragOperation();
        }
        else if (ImGui.IsMouseReleased(0) && !_isInProgress)
        {
            // This happens after shake
            //_draggedItemIds.Clear();
        }

        var wasDraggingRight = ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Length() > UserSettings.Config.ClickThreshold;
        if (ImGui.IsMouseReleased(ImGuiMouseButton.Right)
            && !wasDraggingRight
            && ImGui.IsItemHovered()
            && !_nodeSelection.IsNodeSelected(item))
        {
            item.Select(_nodeSelection);
        }
    }

    internal void UpdateDragging(GraphUiContext context)
    {
        var composition = context.CompositionOp;
        if (composition == null)
            return;
        
        var snappingChanged = HandleSnappedDragging(context.Canvas, composition);
        if (!snappingChanged)
            return;
        
        HandleUnsnapAndCollapse(composition);
        _layout.FlagAsChanged();

        if (!_snapping.IsSnapped)
            return;
        
        if (_snapping.IsInsertion)
        {
            if (TrySplitInsert(composition))
                _layout.FlagAsChanged();
        }
        else if (TryCreateNewConnectionFromSnap(composition))
        {
            _layout.FlagAsChanged();
        }
    }

    /// <summary>
    /// Update dragged items and use anchor definitions to identify and use potential snap targets
    /// </summary>
    private bool HandleSnappedDragging(ICanvas canvas, Instance composition)
    {
        var dl = ImGui.GetWindowDrawList();
        if (!ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            {
                var timeSincePress = ImGui.GetTime() - _mousePressedTime;
                const float longTapDuration = 0.3f;
                var longTapProgress = (float)(timeSincePress / longTapDuration);
                if (longTapProgress < 1)
                {
                    dl.AddCircle(ImGui.GetMousePos(), 100 * (1 - longTapProgress), Color.White.Fade(MathF.Pow(longTapProgress, 3)));
                }

                // Restart after longTap
                else if (_longTapItemId != Guid.Empty)
                {
                    if (_layout.Items.TryGetValue(_longTapItemId, out var item))
                    {
                        item.Select(_nodeSelection);
                        _draggedItemIds.Clear();
                        _draggedItemIds.Add(_longTapItemId);
                        PrepareFrame();
                        StartDragOperation(composition);
                    }
                }
            }

            _hasDragged = false;
            return false;
        }

        if (!_hasDragged)
        {
            _dragStartPosInOpOnCanvas = canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
            _hasDragged = true;
        }

        var mousePosOnCanvas = canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
        var requestedDeltaOnCanvas = mousePosOnCanvas - _dragStartPosInOpOnCanvas;

        var dragExtend = MagGraphItem.GetItemsBounds(_draggedItems);
        dragExtend.Expand(SnapThreshold * canvas.Scale.X);

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
            if (_draggedItemIds.Contains(otherItem.Id) || !dragExtend.Overlaps(otherItem.Area))
                continue;

            overlappingItems.Add(otherItem);
        }

        // Move back to non-snapped position
        foreach (var n in _draggedItems)
        {
            n.PosOnCanvas -= _lastAppliedOffset; // Move to position
            n.PosOnCanvas += requestedDeltaOnCanvas; // Move to request position
        }

        _lastAppliedOffset = requestedDeltaOnCanvas;
        _snapping.Reset();

        foreach (var ip in SplitInsertionPoints)
        {
            var insertionAnchorItem = _draggedItems.FirstOrDefault(i => i.Id == ip.InputItemId);
            if (insertionAnchorItem == null)
                continue;

            foreach (var otherItem in overlappingItems)
            {
                _snapping.TestItemsForInsertion(otherItem, insertionAnchorItem, ip);
            }
        }

        foreach (var otherItem in overlappingItems)
        {
            foreach (var draggedItem in _draggedItems)
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
                       canvas.TransformPosition(mousePosOnCanvas) + _canvas.TransformDirection(bestSnapDelta),
                       Color.White);

            var snapTargetPos = mousePosOnCanvas + bestSnapDelta;
            if (Vector2.Distance(snapTargetPos, LastSnapPositionOnCanvas) > 2) // ugh. Magic number
            {
                snapPositionChanged = true;
                LastSnapTime = ImGui.GetTime();
                LastSnapPositionOnCanvas = snapTargetPos;
            }

            foreach (var n in _draggedItems)
            {
                n.PosOnCanvas += bestSnapDelta;
            }

            _lastAppliedOffset += bestSnapDelta;
            //Log.Debug("Snapped! " + LastSnapPositionOnCanvas);
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

    private void StartDragOperation(Instance composition)
    {
        var snapGraphItems = _draggedItems.Select(i => i as ISelectableCanvasObject).ToList();

        GraphUiContext.MacroCommand = new MacroCommand("Move nodes");
        GraphUiContext._moveElementsCommand = new ModifyCanvasElementsCommand(composition.Symbol.Id, snapGraphItems, _nodeSelection);
        GraphUiContext.MacroCommand.AddExecutedCommandForUndo(GraphUiContext._moveElementsCommand);

        _lastAppliedOffset = Vector2.Zero;
        _isInProgress = true;
        _hasDragged = false;

        InitSplitInsertionPoints(_draggedItems);

        InitPrimaryDraggedOutput();
    }

    /// <summary>
    /// Reset to avoid accidental dragging of previous elements 
    /// </summary>
    private void StopDragOperation()
    {
        _isInProgress = false;
        //_moveElementsCommand = null;
        //_macroCommand = null;

        _lastAppliedOffset = Vector2.Zero;

        //DraggedPrimaryOutputType = null;
        SplitInsertionPoints.Clear();
    }

    /// <summary>
    /// This will reset the state including all highlights and indicators 
    /// </summary>
    internal void Reset()
    {
        PrimaryOutputItem = null;
        PrimaryOutputItemId = Guid.Empty;
        DraggedPrimaryOutputType = null;

        _draggedNodeId = Guid.Empty;
        _draggedItemIds.Clear();
    }

    /// <summary>
    /// Handles op disconnection and collapsed
    /// </summary>
    private void HandleUnsnapAndCollapse(Instance composition)
    {
        if (GraphUiContext.MacroCommand == null)
            return;

        var unsnappedConnections = new List<MagGraphConnection>();

        // Delete unsnapped connections
        foreach (var mc in _layout.MagConnections)
        {
            if (!_snappedBorderConnectionHashes.Contains(mc.ConnectionHash))
                continue;

            unsnappedConnections.Add(mc);

            var connection = new Symbol.Connection(mc.SourceItem.Id,
                                                   mc.SourceOutput.Id,
                                                   mc.TargetItem.Id,
                                                   mc.TargetItem.InputLines[mc.InputLineIndex].Input.Id);

            GraphUiContext.MacroCommand.AddAndExecCommand(new DeleteConnectionCommand(composition.Symbol, connection, 0));
            mc.IsUnlinked = true;
        }
        
        if (TryCollapseDragFromVerticalStack(composition, unsnappedConnections))
            return;

        TryCollapseDisconnectedInputs(composition, unsnappedConnections);
    }

    private bool TryCollapseDragFromVerticalStack(Instance composition, List<MagGraphConnection> unsnappedConnections)
    {
        if (unsnappedConnections.Count <= 1)
            return false;
        
        // Find collapses
        var list = new List<SnapCollapseConnectionPair>();
        var potentialLinks = new List<MagGraphConnection>();

        // Collapse ops dragged from vertical stack...
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
        var movableItems = MoveToFillGaps(pair, potentialMovers, true);
        if (movableItems.Count == 0)
            return false;

        var affectedItemsAsNodes = movableItems.Select(i => i as ISelectableCanvasObject).ToList();
        var newMoveComment = new ModifyCanvasElementsCommand(composition.Symbol.Id, affectedItemsAsNodes, _nodeSelection);
        GraphUiContext.MacroCommand.AddExecutedCommandForUndo(newMoveComment);

        MoveToFillGaps(pair, movableItems, false);
        newMoveComment.StoreCurrentValues();

        GraphUiContext.MacroCommand.AddAndExecCommand(new AddConnectionCommand(composition.Symbol,
                                                                    new Symbol.Connection(pair.Ca.SourceItem.Id,
                                                                                          pair.Ca.SourceOutput.Id,
                                                                                          pair.Cb.TargetItem.Id,
                                                                                          pair.Cb.TargetInput.Id),
                                                                    0));
        return true;
    }
    
    
    private bool TryCollapseDisconnectedInputs(Instance composition, List<MagGraphConnection> unsnappedConnections)
    {
        if (unsnappedConnections.Count == 0)
            return false;
        
        var snappedItems = new HashSet<MagGraphItem>();
        foreach (var mc in unsnappedConnections)
        {
            CollectSnappedItems(mc.TargetItem, snappedItems);
        }
        
        // Collapse ops dragged from vertical stack...
        var collapseLines = new HashSet<float>();
        foreach (var mc in unsnappedConnections)
        {
            if (mc.Style == MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal
                && mc.InputLineIndex > 0
                )
            {
                collapseLines.Add(mc.SourcePos.Y);
            }
        }

        if (collapseLines.Count == 0)
            return false;

        foreach (var y in collapseLines.OrderDescending())
        {
            MoveSnappedItemsVertically(composition,
                                       snappedItems,
                                       y,
                                       -MagGraphItem.GridSize.Y
                                      );
        }
        
        return true;
    }
    
    

    ///<summary>
    /// Iterate through gap lines and move items below upwards
    /// </summary>
    private static HashSet<MagGraphItem> MoveToFillGaps(SnapCollapseConnectionPair pair, HashSet<MagGraphItem> movableItems, bool dryRun)
    {
        var minY = pair.Ca.SourcePos.Y;
        var maxY = pair.Cb.TargetPos.Y;
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
                if (!(item.PosOnCanvas.Y > middleSnapLineY))
                    continue;

                affectedItems.Add(item);

                if (!dryRun)
                    item.PosOnCanvas -= new Vector2(0, lineWidth);
            }
        }

        return affectedItems;
    }

    private sealed record SnapCollapseConnectionPair(MagGraphConnection Ca, MagGraphConnection Cb);

    ///<summary>
    /// Search for potential new connections through snapping
    /// </summary>
    private bool TryCreateNewConnectionFromSnap(Instance composition)
    {
        if (!_snapping.IsSnapped || GraphUiContext.MacroCommand == null)
            return false;

        var newConnections = new List<Symbol.Connection>();
        foreach (var draggedItem in _draggedItems)
        {
            foreach (var otherItem in _layout.Items.Values)
            {
                if (_draggedItemIds.Contains(otherItem.Id))
                    continue;

                GetPotentialConnectionsAfterSnap(ref newConnections, draggedItem, otherItem);
                GetPotentialConnectionsAfterSnap(ref newConnections, otherItem, draggedItem);
            }
        }

        foreach (var newConnection in newConnections)
        {
            if (Structure.CheckForCycle(composition.Symbol, newConnection))
            {
                Log.Debug("Sorry, this connection would create a cycle. (4)");
                continue;
            }

            GraphUiContext.MacroCommand.AddAndExecCommand(new AddConnectionCommand(composition.Symbol, newConnection, 0));
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
    private bool TrySplitInsert(Instance composition)
    {
        if (!_snapping.IsSnapped || GraphUiContext.MacroCommand == null || _snapping.BestA == null)
            return false;

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

        GraphUiContext.MacroCommand.AddAndExecCommand(new DeleteConnectionCommand(composition.Symbol,
                                                                       connection.AsSymbolConnection(),
                                                                       0));
        GraphUiContext.MacroCommand.AddAndExecCommand(new AddConnectionCommand(composition.Symbol,
                                                                    new Symbol.Connection(connection.SourceItem.Id,
                                                                                          connection.SourceOutput.Id,
                                                                                          insertionPoint.InputItemId,
                                                                                          insertionPoint.InputId
                                                                                         ), 0));

        GraphUiContext.MacroCommand.AddAndExecCommand(new AddConnectionCommand(composition.Symbol,
                                                                    new Symbol.Connection(insertionPoint.OutputItemId,
                                                                                          insertionPoint.OutputId,
                                                                                          connection.TargetItem.Id,
                                                                                          connection.TargetInput.Id
                                                                                         ), 0));

        MoveSnappedItemsVertically(composition,
                                   CollectSnappedItems( _snapping.BestA),
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
    private bool MoveSnappedItemsVertically(Instance composition, HashSet<MagGraphItem> snappedItems, float yThreshold, float yDistance)
    {
        var movableItems = new List<MagGraphItem>();
        foreach (var otherItem in snappedItems)
        {
            if (otherItem.PosOnCanvas.Y > yThreshold)
            {
                movableItems.Add(otherItem);
            }
        }

        if (movableItems.Count == 0 || GraphUiContext.MacroCommand == null)
            return false;

        // Move items down...
        var affectedItemsAsNodes = movableItems.Select(i => i as ISelectableCanvasObject).ToList();
        var newMoveComment = new ModifyCanvasElementsCommand(composition.Symbol.Id, affectedItemsAsNodes, _nodeSelection);
        GraphUiContext.MacroCommand.AddExecutedCommandForUndo(newMoveComment);

        foreach (var item in affectedItemsAsNodes)
        {
            item.PosOnCanvas += new Vector2(0, yDistance);
        }

        newMoveComment.StoreCurrentValues();
        return true;
    }

    private void InitPrimaryDraggedOutput()
    {
        DraggedPrimaryOutputType = null;
        PrimaryOutputItemId = Guid.Empty;
        PrimaryOutputItem = null;
        ItemForInputSelection = null;
        var primaryOutputItem = FindPrimaryOutputItem();
        if (primaryOutputItem == null)
            return;

        DraggedPrimaryOutputType = primaryOutputItem.PrimaryType;

        if (primaryOutputItem.InputLines.Length == 0)
            return;

        PrimaryOutputItemId = primaryOutputItem.Id;
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
    private static MagGraphItem? FindPrimaryOutputItem()
    {
        if (_draggedItems.Count == 0)
            return null;

        if (_draggedItems.Count == 1)
            return _draggedItems.First();

        var itemsOrderedByX = _draggedItems.OrderByDescending(c => c.PosOnCanvas.X);
        var rightItem = itemsOrderedByX.First();

        // Check if there are multiple items on right edge column...
        var rightColumnItemCount = 0;
        foreach (var i in _draggedItems)
        {
            if (Math.Abs(i.PosOnCanvas.X - rightItem.PosOnCanvas.X) < SnapTolerance)
                rightColumnItemCount++;
        }

        if (rightColumnItemCount > 1)
            return null;

        if (_draggedItems.Count != CollectSnappedItems(rightItem).Count)
            return null;

        return rightItem;
    }

    private bool TryInitializeInputSelectionPicker()
    {
        if (PrimaryOutputItem == null)
            return false;

        foreach (var otherItem in _layout.Items.Values)
        {
            if (otherItem == PrimaryOutputItem)
                continue;

            if (!otherItem.Area.Contains(PeekAnchorInCanvas))
                continue;

            ItemForInputSelection = otherItem;
            return true;
        }

        return false;
    }

    /// <summary>
    /// After snapping picking an hidden input field for connection, this
    /// method can be called to...
    /// - move the dragged items to the snapped position
    /// - move all other relevant snapped items down
    /// - create the connection
    /// </summary>
    internal void TryConnectHiddenInput(IInputUi targetInputUi)
    {
        var composition = _context.CompositionOp;
        if (composition == null)
            return;
        
        Debug.Assert(PrimaryOutputItem != null && ItemForInputSelection != null);
        Debug.Assert(GraphUiContext.MacroCommand != null);
        Debug.Assert(ItemForInputSelection.Variant == MagGraphItem.Variants.Operator); // This will bite us later...

        if (PrimaryOutputItem.OutputLines.Length == 0)
        {
            Log.Warning("no visible output to connect?");
            return;
        }
        
        // Create connection
        var connectionToAdd = new Symbol.Connection(PrimaryOutputItemId,
                                                    PrimaryOutputItem.OutputLines[0].Id,
                                                    ItemForInputSelection.Id,
                                                    targetInputUi.Id);

        if (Structure.CheckForCycle(composition.Symbol, connectionToAdd))
        {
            Log.Debug("Sorry, this connection would create a cycle.");
            return;
        }

        GraphUiContext.MacroCommand.AddAndExecCommand(new AddConnectionCommand(composition.Symbol,
                                                                    connectionToAdd,
                                                                    0));

        // Find insertion index
        var inputLineIndex = 0;
        foreach (var input in ItemForInputSelection.Instance.Inputs)
        {
            if (inputLineIndex < ItemForInputSelection.InputLines.Length
                && input.Id == ItemForInputSelection.InputLines[inputLineIndex].Input.Id)
                inputLineIndex++;

            if (input.Id == targetInputUi.InputDefinition.Id)
                break;
        }

        MoveSnappedItemsVertically(composition,
                                   CollectSnappedItems(ItemForInputSelection),
                                   ItemForInputSelection.PosOnCanvas.Y + MagGraphItem.GridSize.Y * (inputLineIndex - 0.5f),
                                   MagGraphItem.GridSize.Y);

        // Snap items to input location (we assume that all dragged items are snapped...)
        var targetPos = ItemForInputSelection.PosOnCanvas
                        + new Vector2(-PrimaryOutputItem.Size.X,
                                      (inputLineIndex) * MagGraphItem.GridSize.Y);

        var moveDelta = targetPos - PrimaryOutputItem.PosOnCanvas;

        var affectedItemsAsNodes = _draggedItems.Select(i => i as ISelectableCanvasObject).ToList();
        var newMoveComment = new ModifyCanvasElementsCommand(composition.Symbol.Id, affectedItemsAsNodes, _nodeSelection);
        GraphUiContext.MacroCommand.AddExecutedCommandForUndo(newMoveComment);

        foreach (var item in affectedItemsAsNodes)
        {
            item.PosOnCanvas += moveDelta;
        }

        newMoveComment.StoreCurrentValues();

        // Complete drag interaction
        Reset();
    }

    /// <summary>
    /// Add snapped items to the given set or create new set
    /// </summary>
    private static HashSet<MagGraphItem> CollectSnappedItems(MagGraphItem rootItem, HashSet<MagGraphItem>? set = null)
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

    public static bool IsItemDragged(MagGraphItem item) => _draggedItemIds.Contains(item.Id);

    public double LastSnapTime = double.NegativeInfinity;
    public Vector2 LastSnapPositionOnCanvas;

    internal Type? DraggedPrimaryOutputType;
    internal MagGraphItem? PrimaryOutputItem;
    internal MagGraphItem? ItemForInputSelection;
    internal Guid PrimaryOutputItemId;

    internal Vector2 PeekAnchorInCanvas => PrimaryOutputItem == null
                                               ? Vector2.Zero
                                               : new Vector2(PrimaryOutputItem.Area.Max.X - MagGraphItem.GridSize.Y * 0.25f,
                                                             PrimaryOutputItem.Area.Min.Y + MagGraphItem.GridSize.Y * 0.5f);

    internal readonly List<SplitInsertionPoint> SplitInsertionPoints = [];

    private Vector2 _lastAppliedOffset;
    private const float SnapThreshold = 30;
    private readonly List<MagGraphConnection> _borderConnections = [];
    private bool _wasSnapped;
    private static readonly MagItemMovement.Snapping _snapping = new();

    private Guid _longTapItemId = Guid.Empty;
    private double _mousePressedTime;

    private static bool _isInProgress;
    private static bool _hasDragged;
    private Vector2 _dragStartPosInOpOnCanvas;

    private static Guid _draggedNodeId = Guid.Empty;
    private static readonly HashSet<MagGraphItem> _draggedItems = [];
    private static readonly HashSet<Guid> _draggedItemIds = [];

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