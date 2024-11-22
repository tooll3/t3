#nullable enable

using System.Diagnostics.CodeAnalysis;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using Vector2 = System.Numerics.Vector2;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

// ReSharper disable UseWithExpressionToCopyStruct

namespace T3.Editor.Gui.MagGraph.Ui;

/// <summary>
/// 
/// </summary>
/// <remarks>
/// Things would be slightly more efficient if this would would use SnapGraphItems. However this would
/// prevent us from reusing fence selection. Enforcing this to be used for dragging inputs and outputs
/// makes this class unnecessarily complex.
/// </remarks>
internal sealed class MagItemMovement
{
    public MagItemMovement(MagGraphCanvas magGraphCanvas, MagGraphLayout layout, NodeSelection nodeSelection)
    {
        _canvas = magGraphCanvas;
        _layout = layout;
        _nodeSelection = nodeSelection;
    }

    /** During connection operations we want to indicate operator if matching but hidden inputs */
    public Type? ConnectionTargetType; // = typeof(float);

    /// <summary>
    /// We can't assume the the layout model structure to be stable across frames.
    /// We use Guids to carry over the information for dragged items.
    /// </summary>
    ///
    /// <remarks>
    /// This can eventually be optimized by having a structure counter or a hasChanged flag in layout.
    /// </remarks>
    public void PrepareFrame()
    {
        // Derive _draggedItems from ids
        _draggedItems.Clear();
        //PrimaryOutputItem = null;

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
        if (ImGui.IsMouseReleased(0) && _macroCommand != null)
        {
            StopDragOperation();
        }
    }

    /// <summary>
    /// NOTE: This has to be called for ALL movable elements (ops, inputs, outputs) and directly after ImGui.Item
    /// </summary>
    public void HandleForItem(MagGraphItem item, MagGraphCanvas canvas, Instance composition)
    {
        var isActiveNode = item.Id == _draggedNodeId;
        var clickedDown = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup) && ImGui.IsMouseClicked(ImGuiMouseButton.Left);

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

                PrepareFrame();
            }
            else
            {
                CollectSnappedDragItems(item);
            }

            _mousePressedTime = ImGui.GetTime();

            StartDragOperation(composition);

            //ShakeDetector.ResetShaking();
        }
        // Update dragging...
        else if (isActiveNode && ImGui.IsMouseDown(ImGuiMouseButton.Left) && _macroCommand != null)
        {
            // TODO: Implement shake disconnect later

            var snappingChanged = HandleSnappedDragging(canvas, composition);
            if (snappingChanged)
            {
                HandleUnsnapAndCollapse(composition);
                _layout.FlagAsChanged();

                if (_snapping.IsSnapped)
                {
                    if (_snapping.IsInsertion)
                    {
                        if (TryToSplitInsert(composition))
                            _layout.FlagAsChanged();
                    }
                    else if (TryCreateNewConnectionFromSnap(composition))
                    {
                        _layout.FlagAsChanged();
                    }
                }
            }
        }
        // Release and complete dragging
        else if (isActiveNode && ImGui.IsMouseReleased(0) && _macroCommand != null)
        {
            if (_draggedNodeId != item.Id)
                return;

            var singleDraggedNode = (_draggedItems.Count == 1) ? _draggedItems.First() : null;

            var wasDragging = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).LengthSquared() > UserSettings.Config.ClickThreshold;
            if (wasDragging)
            {
                _moveElementsCommand?.StoreCurrentValues();

                if (singleDraggedNode != null
                    && ConnectionSplitHelper.BestMatchLastFrame != null)
                {
                    //var instanceForSymbolChildUi = composition.Children.SingleOrDefault(child => child.SymbolChildId == graphItem.SymbolChildUi.Id);
                    // TODO: Implement later
                    // ConnectionMaker.SplitConnectionWithDraggedNode(graphItem.SymbolChildUi,
                    //                                                ConnectionSplitHelper.BestMatchLastFrame.Connection,
                    //                                                graphItem.Instance,
                    //                                                _modifyCommand);
                    _macroCommand = null;
                }
                else
                {
                    UndoRedoStack.Add(_macroCommand);
                }

                //FieldHoveredItem = PrimaryOutputItem;
                if (PrimaryOutputItem != null)
                {
                    MagGraphItem? hoveredItem = null;
                    foreach (var otherItem in _layout.Items.Values)
                    {
                        if (!otherItem.Area.Contains(PeekAnchorInCanvas))
                            continue;
                        hoveredItem = otherItem;
                        break;
                    }

                    if (hoveredItem != null)
                    {
                        FieldHoveredItem = hoveredItem;
                    }
                }
            }
            else
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

            StopDragOperation();
        }
        else if (ImGui.IsMouseReleased(0) && _macroCommand == null)
        {
            // This happens after shake
            _draggedItemIds.Clear();
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

    /// <summary>
    /// Handles op disconnection and collapsed
    /// </summary>
    private void HandleUnsnapAndCollapse(Instance composition)
    {
        if (_macroCommand == null)
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

            _macroCommand.AddAndExecCommand(new DeleteConnectionCommand(composition.Symbol, connection, 0));
            mc.IsUnlinked = true;
        }

        // find collapses
        var list = new List<SnapCollapseConnectionPair>();
        var potentialLinks = new List<MagGraphConnection>();

        foreach (var ca in unsnappedConnections)
        {
            if (ca.Style == MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical)
            {
                potentialLinks.Clear();
                foreach (var cb in unsnappedConnections)
                {
                    if (ca != cb
                        && cb.Style == MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical
                        && cb.SourcePos.Y > ca.SourcePos.Y
                        && Math.Abs(cb.SourcePos.X - ca.SourcePos.X) < SnapTolerance
                        && cb.Type == ca.Type)
                    {
                        potentialLinks.Add(cb);
                    }
                }

                if (potentialLinks.Count == 1)
                {
                    list.Add(new SnapCollapseConnectionPair(ca, potentialLinks[0]));
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
        }
        else if (list.Count == 1)
        {
            var pair = list[0];

            if (Structure.CheckForCycle(pair.Ca.SourceItem.Instance, pair.Cb.TargetItem.Id))
            {
                Log.Debug("Sorry, this connection would create a cycle. (1)");
                return;
            }

            var potentialMovers = CollectSnappedItems(pair.Cb.TargetItem);

            // Clarify if the subset of items snapped to lower target op is sufficient to fill most gaps

            // First find movable items and then create command with movements
            var movableItems = MoveToFillGaps(pair, potentialMovers, true);
            if (movableItems.Count == 0)
                return;

            var affectedItemsAsNodes = movableItems.Select(i => i as ISelectableCanvasObject).ToList();
            var newMoveComment = new ModifyCanvasElementsCommand(composition.Symbol.Id, affectedItemsAsNodes, _nodeSelection);
            _macroCommand.AddExecutedCommandForUndo(newMoveComment);

            MoveToFillGaps(pair, movableItems, false);
            newMoveComment.StoreCurrentValues();

            _macroCommand.AddAndExecCommand(new AddConnectionCommand(composition.Symbol,
                                                                     new Symbol.Connection(pair.Ca.SourceItem.Id,
                                                                                           pair.Ca.SourceOutput.Id,
                                                                                           pair.Cb.TargetItem.Id,
                                                                                           pair.Cb.TargetInput.Id),
                                                                     0));
        }
    }

    /** Iterate through gap lines and move items below upwards */
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

    /** Search for potential new connections through snapping */
    private bool TryToSplitInsert(Instance composition)
    {
        if (!_snapping.IsSnapped || _macroCommand == null || _snapping.BestA == null)
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

        _macroCommand.AddAndExecCommand(new DeleteConnectionCommand(composition.Symbol,
                                                                    connection.AsSymbolConnection(),
                                                                    0));
        _macroCommand.AddAndExecCommand(new AddConnectionCommand(composition.Symbol,
                                                                 new Symbol.Connection(connection.SourceItem.Id,
                                                                                       connection.SourceOutput.Id,
                                                                                       insertionPoint.InputItemId,
                                                                                       insertionPoint.InputId
                                                                                      ), 0));

        _macroCommand.AddAndExecCommand(new AddConnectionCommand(composition.Symbol,
                                                                 new Symbol.Connection(insertionPoint.OutputItemId,
                                                                                       insertionPoint.OutputId,
                                                                                       connection.TargetItem.Id,
                                                                                       connection.TargetInput.Id
                                                                                      ), 0));

        // Find movable items...
        var snappedItems = CollectSnappedItems(_snapping.BestA);
        var movableItems = new List<MagGraphItem>();
        foreach (var otherItem in snappedItems)
        {
            if (otherItem.PosOnCanvas.Y > _snapping.OutAnchorPos.Y - MagGraphItem.GridSize.Y / 2)
            {
                movableItems.Add(otherItem);
            }
        }

        if (movableItems.Count == 0)
            return false;

        // Move items down...
        var affectedItemsAsNodes = movableItems.Select(i => i as ISelectableCanvasObject).ToList();
        var newMoveComment = new ModifyCanvasElementsCommand(composition.Symbol.Id, affectedItemsAsNodes, _nodeSelection);
        _macroCommand.AddExecutedCommandForUndo(newMoveComment);

        foreach (var item in affectedItemsAsNodes)
        {
            item.PosOnCanvas += new Vector2(0, insertionPoint.Distance);
        }

        newMoveComment.StoreCurrentValues();

        return true;
    }

    /** Search for potential new connections through snapping */
    private bool TryCreateNewConnectionFromSnap(Instance composition)
    {
        if (!_snapping.IsSnapped || _macroCommand == null)
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

            _macroCommand.AddAndExecCommand(new AddConnectionCommand(composition.Symbol, newConnection, 0));
        }

        return newConnections.Count > 0;
    }

    private void StartDragOperation(Instance composition)
    {
        var snapGraphItems = _draggedItems.Select(i => i as ISelectableCanvasObject).ToList();
        _macroCommand = new MacroCommand("Move nodes");
        _moveElementsCommand = new ModifyCanvasElementsCommand(composition.Symbol.Id, snapGraphItems, _nodeSelection);
        _macroCommand.AddExecutedCommandForUndo(_moveElementsCommand);

        DraggedPrimaryOutputType = null;

        _lastAppliedOffset = Vector2.Zero;
        _isDragging = false;

        //UpdateBorderConnections(_draggedItems);
        UpdateSplitInsertionPoints(_draggedItems);

        PrimaryOutputItemId = Guid.Empty;
        PrimaryOutputItem = null;
        FieldHoveredItem = null;
        var primaryOutputItem = FindPrimaryOutputItem();
        if (primaryOutputItem == null)
        {
            return;
        }

        //var item = _draggedItems.First();

        DraggedPrimaryOutputType = primaryOutputItem.PrimaryType;

        if (primaryOutputItem.InputLines.Length == 0)
            return;

        PrimaryOutputItemId = primaryOutputItem.Id;
    }

    /** Snapping an item onto a hidden parameter is a complicated ui problem.
     * To allow this interaction we add "peek" anchor indicator to the dragged item set.
     * If this is dropped without snapping onto an operator with hidden parameters of the matching type,
     * we present cables.gl inspired picker interface.
     */
    /**
     * Set primary types to indicate targets for dragging
     *
     * This part is a little fishy. To have "some" solution we try to identify dragged
     *    constellations that has a single free horizontal output anchor on the left column.
     *
     *     -
     *     -X
     *
     */
    private MagGraphItem? FindPrimaryOutputItem()
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

    //private sealed record PeekAnchor(Guid ItemId, Type Type);
    //private Pick

    /// <summary>
    /// Reset to avoid accidental dragging of previous elements 
    /// </summary>
    private void StopDragOperation()
    {
        _moveElementsCommand = null;
        _macroCommand = null;

        _lastAppliedOffset = Vector2.Zero;

        _draggedNodeId = Guid.Empty;
        _draggedItemIds.Clear();
        //DraggedPrimaryOutputType = null;
        ConnectionTargetType = null;
        SplitInsertionPoints.Clear();
    }

    /** Update dragged items and use anchor definitions to identify and use potential snap targets */
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

            _isDragging = false;
            return false;
        }

        if (!_isDragging)
        {
            _dragStartPosInOpOnCanvas = canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
            _isDragging = true;
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
        else
        {
            LastSnapPositionOnCanvas = Vector2.Zero;
            LastSnapTime = double.NegativeInfinity;
        }

        var snappingChanged = _snapping.IsSnapped != _wasSnapped || _snapping.IsSnapped && snapPositionChanged;
        _wasSnapped = _snapping.IsSnapped;
        return snappingChanged;
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

    /**
     * When starting a new drag operation, we try to identify border input anchors of the dragged items,
     * that can be used to insert them between other snapped items.
     */
    private void UpdateSplitInsertionPoints(HashSet<MagGraphItem> draggedItems)
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

    [SuppressMessage("ReSharper", "NotAccessedField.Local")]
    private sealed class Snapping
    {
        public float BestDistance;
        public MagGraphItem? BestA;
        public MagGraphItem? BestB;
        public MagGraphItem.Directions Direction;
        public int InputLineIndex;
        public int MultiInputIndex;
        public int OutLineIndex;
        public Vector2 OutAnchorPos;
        public Vector2 InputAnchorPos;
        public bool Reverse;
        public bool IsSnapped => BestDistance < MagGraphItem.LineHeight * (IsInsertion ? 0.35 : 0.5f);
        public bool IsInsertion;
        public SplitInsertionPoint? InsertionPoint;

        public void TestItemsForSnap(MagGraphItem a, MagGraphItem b, bool revert, MagGraphCanvas canvas)
        {
            MagGraphConnection? inConnection;

            int aOutLineIndex, bInputLineIndex;
            for (bInputLineIndex = 0; bInputLineIndex < b.InputLines.Length; bInputLineIndex++)
            {
                ref var bInputLine = ref b.InputLines[bInputLineIndex];
                inConnection = bInputLine.ConnectionIn;

                for (aOutLineIndex = 0; aOutLineIndex < a.OutputLines.Length; aOutLineIndex++)
                {
                    ref var outputLine = ref a.OutputLines[aOutLineIndex]; // Avoid copying data from array
                    if (bInputLine.Type != outputLine.Output.ValueType)
                        continue;

                    // vertical
                    if (aOutLineIndex == 0 && bInputLineIndex == 0)
                    {
                        TestAndKeepPositionsForSnapping(ref outputLine,
                                                        0,
                                                        MagGraphItem.Directions.Vertical,
                                                        new Vector2(a.Area.Min.X + MagGraphItem.WidthHalf, a.Area.Max.Y),
                                                        new Vector2(b.Area.Min.X + MagGraphItem.WidthHalf, b.Area.Min.Y));
                    }

                    // horizontal
                    if (outputLine.Output.ValueType == bInputLine.Input.ValueType)
                    {
                        TestAndKeepPositionsForSnapping(ref outputLine,
                                                        bInputLine.MultiInputIndex,
                                                        MagGraphItem.Directions.Horizontal,
                                                        new Vector2(a.Area.Max.X, a.Area.Min.Y + (0.5f + outputLine.VisibleIndex) * MagGraphItem.LineHeight),
                                                        new Vector2(b.Area.Min.X, b.Area.Min.Y + (0.5f + bInputLine.VisibleIndex) * MagGraphItem.LineHeight));
                    }
                }
            }

            //Direction = MagGraphItem.Directions.Horizontal;
            return;

            void TestAndKeepPositionsForSnapping(ref MagGraphItem.OutputLine outputLine,
                                                 int multiInputIndexIfValid,
                                                 MagGraphItem.Directions directionIfValid,
                                                 Vector2 outPos,
                                                 Vector2 inPos)
            {
                // If input is connected the only valid output is the one with the connection line
                if (inConnection != null && outputLine.ConnectionsOut.All(c => c != inConnection))
                    return;

                ShowDebugLine(outPos, inPos, a.PrimaryType);

                var d = Vector2.Distance(outPos, inPos);
                if (d >= BestDistance)
                    return;

                BestDistance = d;
                OutAnchorPos = outPos;
                InputAnchorPos = inPos;
                OutLineIndex = aOutLineIndex;
                InputLineIndex = bInputLineIndex;
                BestA = a;
                BestB = b;
                Reverse = revert;
                Direction = directionIfValid;
                IsInsertion = false;
                MultiInputIndex = multiInputIndexIfValid;
            }

            void ShowDebugLine(Vector2 outPos, Vector2 inPos, Type connectionType)
            {
                if (!canvas.ShowDebug)
                    return;

                var drawList = ImGui.GetForegroundDrawList();
                var uiPrimaryColor = TypeUiRegistry.GetPropertiesForType(connectionType).Color;
                drawList.AddLine(canvas.TransformPosition(outPos),
                                 canvas.TransformPosition(inPos),
                                 uiPrimaryColor.Fade(0.4f));

                drawList.AddCircleFilled(canvas.TransformPosition(inPos), 6, uiPrimaryColor.Fade(0.4f));
            }
        }

        public void TestItemsForInsertion(MagGraphItem item, MagGraphItem insertionAnchorItem, SplitInsertionPoint insertionPoint)
        {
            if (item.InputLines.Length < 1)
                return;

            var mainInput = item.InputLines[0];

            // Vertical
            if (mainInput.ConnectionIn == null
                || mainInput.Type != insertionPoint.Type
                || mainInput.ConnectionIn.Style != MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical)
            {
                return;
            }

            var inputPos = item.PosOnCanvas + new Vector2(MagGraphItem.GridSize.X / 2, 0);
            var insertionAnchorPos = insertionAnchorItem.PosOnCanvas + new Vector2(MagGraphItem.GridSize.X / 2, 0);
            var d = Vector2.Distance(insertionAnchorPos, inputPos);

            if (d >= BestDistance)
                return;

            BestDistance = d;
            OutAnchorPos = inputPos;
            InputAnchorPos = insertionAnchorPos;
            OutLineIndex = 0;
            InputLineIndex = 0;
            BestA = item;
            BestB = null;
            Reverse = false;
            Direction = MagGraphItem.Directions.Vertical;
            MultiInputIndex = 0;
            IsInsertion = true;
            InsertionPoint = insertionPoint;
        }

        public void Reset()
        {
            BestDistance = float.PositiveInfinity;
        }
    }

    private static void CollectSnappedDragItems(MagGraphItem rootItem)
    {
        _draggedItems.Clear();
        CollectSnappedItems(rootItem, _draggedItems);

        _draggedItemIds.Clear();
        foreach (var item in _draggedItems)
        {
            _draggedItemIds.Add(item.Id);
        }
    }

    /// <summary>
    /// Add snapped items to the give set or create new set
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

    // Related to hidden inputs connectivity
    internal Type? DraggedPrimaryOutputType;
    internal MagGraphItem? PrimaryOutputItem;
    internal MagGraphItem? FieldHoveredItem;
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
    private static readonly Snapping _snapping = new();

    //private MagGraphItem? _longTapItem;
    private Guid _longTapItemId = Guid.Empty;
    private double _mousePressedTime;

    private static bool _isDragging;
    private Vector2 _dragStartPosInOpOnCanvas;
    private static MacroCommand? _macroCommand;
    private static ModifyCanvasElementsCommand? _moveElementsCommand;
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