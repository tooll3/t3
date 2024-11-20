#nullable enable

using System.Diagnostics.CodeAnalysis;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;
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

    /// <summary>
    /// We can't assume the the layout model structure to be stable across frames.
    /// We use Guids to carry over the information for dragged items.
    /// </summary>
    ///
    /// <remarks>This can eventually be optimized by having a structure counter or
    /// a hasChanged flag in layout.</remarks>
    public void PrepareFrame()
    {
        // derive _draggedItems from ids
        _draggedItems.Clear();
        foreach (var id in _draggedItemIds)
        {
            if (!_layout.Items.TryGetValue(id, out var item))
            {
                Log.Warning("Dragged item no longer valid?");
                continue;
            }

            _draggedItems.Add(item);
        }

        UpdateBorderConnections(_draggedItems);
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
                //_draggedItemIds.Clear();
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
                if (_snapping.IsSnapped)
                {
                    if (TryCreateNewConnectionFromSnap(composition))
                        _layout.FlagAsChanged();
                }
                else
                {
                    HandleUnsnapAndCollapse(composition);
                    _layout.FlagAsChanged();
                }

                //Log.Debug("Snapping changed " + _snapping.IsSnapped);
            }
        }
        // Release and complete dragging
        else if (isActiveNode && ImGui.IsMouseReleased(0) && _macroCommand != null)
        {
            if (_draggedNodeId != item.Id)
                return;

            var singleDraggedNode = (_draggedItems.Count == 1) ? _draggedItems.First() : null;
            _draggedNodeId = Guid.Empty;
            _draggedItemIds.Clear();
            _lastAppliedOffset = Vector2.Zero;

            var wasDragging = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).LengthSquared() > UserSettings.Config.ClickThreshold;
            if (wasDragging)
            {
                _moveElementsCommand.StoreCurrentValues();

                if (singleDraggedNode != null
                    && ConnectionSplitHelper.BestMatchLastFrame != null
                    && singleDraggedNode is MagGraphItem graphItem)
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

            _macroCommand = null;
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
    /// Handles how disconnection and collapsed
    /// </summary>
    private void HandleUnsnapAndCollapse(Instance composition)
    {
        if (_macroCommand == null)
            return;

        var unsnappedConnections = new List<MagGraphConnection>();

        // Remove unsnapped connections
        foreach (var c in _layout.MagConnections)
        {
            if (!_snappedBorderConnections.Contains(c.ConnectionHash))
                continue;

            unsnappedConnections.Add(c);

            //Log.Debug("Snapped border connection has been broken " + c);
            var connection = new Symbol.Connection(c.SourceItem.Id,
                                                   c.SourceOutput.Id,
                                                   c.TargetItem.Id,
                                                   c.TargetItem.InputLines[c.InputLineIndex].Input.Id);

            _macroCommand.AddAndExecCommand(new DeleteConnectionCommand(composition.Symbol, connection, 0));
            c.IsUnlinked = true;
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
                        && Math.Abs(cb.SourcePos.X - ca.SourcePos.X) < SnapTollerance
                        && cb.Type == ca.Type)
                    {
                        potentialLinks.Add(cb);
                    }
                }

                if (potentialLinks.Count == 1)
                {
                    list.Add(new SnapCollapseConnectionPair(ca,potentialLinks[0]));
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
                
                if(!dryRun)
                    item.PosOnCanvas -= new Vector2(0,lineWidth);
            }
        }

        return affectedItems;
    }

    private record SnapCollapseConnectionPair(MagGraphConnection Ca, MagGraphConnection Cb);

    private bool TryCreateNewConnectionFromSnap(Instance composition)
    {
        // Search for potential new connections through snapping
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

        DraggedPrimaryInputType = null;
        DraggedPrimaryOutputType = null;

        _lastAppliedOffset = Vector2.Zero;
        _isDragging = false;

        // Set primary types to indicate targets for dragging
        if (_draggedItemIds.Count != 1)
            return;

        if (_draggedItems.Count != _draggedItemIds.Count)
        {
            Log.Warning("Inconsistent item lists");
            return;
        }

        var item = _draggedItems.First();
        DraggedPrimaryOutputType = item.PrimaryType;

        if (item.InputLines.Length == 0)
            return;

        DraggedPrimaryInputType = item.InputLines[0].Type;
    }

    /// <summary>
    /// Reset to avoid accidental dragging of previous elements 
    /// </summary>
    private void StopDragOperation()
    {
        _moveElementsCommand = null;
        _macroCommand = null;
        _lastAppliedOffset = Vector2.Zero;
        _draggedItemIds.Clear();
    }

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
                // restart after longTap.
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
                    // var snapGraphItems = _draggedItems.Select(i => i as ISelectableCanvasObject).ToList();
                    // _macroCommand = new MacroCommand("Move elements");
                    // _moveElementsCommand = new ModifyCanvasElementsCommand(composition.Symbol.Id, snapGraphItems, _nodeSelection);
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

        //_snapping = new Snapping() { BestDistance = float.PositiveInfinity};
        _snapping.Reset();

        foreach (var otherItem in overlappingItems)
        {
            foreach (var draggedItem in _draggedItems)
            {
                _snapping.TestItemsForSnap(otherItem, draggedItem, false, _canvas);
                _snapping.TestItemsForSnap(draggedItem, otherItem, true, _canvas);
            }
        }

        // Highlight best distance
        if (_canvas.ShowDebug)
        {
            if (_snapping.BestDistance < 500)
            {
                var p1 = _canvas.TransformPosition(_snapping.OutAnchorPos);
                var p2 = _canvas.TransformPosition(_snapping.InputAnchorPos);
                dl.AddLine(p1, p2, UiColors.ForegroundFull.Fade(0.1f), 6);
            }
        }

        // Snapped
        if (_snapping.IsSnapped)
        {
            var bestSnapDelta = !_snapping.Reverse
                                    ? _snapping.OutAnchorPos - _snapping.InputAnchorPos
                                    : _snapping.InputAnchorPos - _snapping.OutAnchorPos;

            dl.AddLine(_canvas.TransformPosition(mousePosOnCanvas),
                       canvas.TransformPosition(mousePosOnCanvas) + _canvas.TransformDirection(bestSnapDelta),
                       Color.White);

            if (Vector2.Distance(_snapping.InputAnchorPos, LastSnapPositionOnCanvas) > 2)
            {
                LastSnapTime = ImGui.GetTime();
                LastSnapPositionOnCanvas = _snapping.InputAnchorPos;
            }

            foreach (var n in _draggedItems)
            {
                n.PosOnCanvas += bestSnapDelta;
            }

            _lastAppliedOffset += bestSnapDelta;
        }
        else
        {
            LastSnapPositionOnCanvas = Vector2.Zero;
            LastSnapTime = double.NegativeInfinity;
        }

        var snappingChanged = _snapping.IsSnapped != _wasSnapped;
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

        _snappedBorderConnections.Clear();

        foreach (var c in _borderConnections)
        {
            if (c.IsSnapped)
                _snappedBorderConnections.Add(c.ConnectionHash);
        }
    }

    private readonly HashSet<int> _snappedBorderConnections = new();

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
                                              MagGraphItem.Directions.Vertical,
                                              new Vector2(a.Area.Min.X + MagGraphItem.WidthHalf, a.Area.Max.Y),
                                              new Vector2(b.Area.Min.X + MagGraphItem.WidthHalf, b.Area.Min.Y));
                }

                // horizontal
                if (outputLine.Output.ValueType == bInputLine.Type)
                {
                    AddPossibleNewConnections(ref result,
                                              ref outputLine,
                                              ref bInputLine,
                                              MagGraphItem.Directions.Horizontal,
                                              new Vector2(a.Area.Max.X, a.Area.Min.Y + (0.5f + outputLine.VisibleIndex) * MagGraphItem.LineHeight),
                                              new Vector2(b.Area.Min.X, b.Area.Min.Y + (0.5f + bInputLine.VisibleIndex) * MagGraphItem.LineHeight));
                }
            }
        }

        return;

        void AddPossibleNewConnections(ref List<Symbol.Connection> newConnections,
                                       ref MagGraphItem.OutputLine outputLine,
                                       ref MagGraphItem.InputLine inputLine,
                                       MagGraphItem.Directions directionIfValid,
                                       Vector2 outPos,
                                       Vector2 inPos)
        {
            if (Vector2.Distance(outPos, inPos) > SnapTollerance)
                return;

            if (inConnection != null)
                return;

            // Clarify if outConnection should also be empty...
            if (outputLine.ConnectionsOut.Count > 0)
            {
                if (outputLine.ConnectionsOut[0].IsSnapped
                    && (outputLine.ConnectionsOut[0].SourcePos - inPos).Length() < SnapTollerance)
                    return;
            }

            newConnections.Add(new Symbol.Connection(
                                                     a.Id,
                                                     outputLine.OutputUi.Id,
                                                     b.Id,
                                                     inputLine.Id
                                                    ));
        }
    }

    [SuppressMessage("ReSharper", "NotAccessedField.Local")]
    private class Snapping
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
        public bool IsSnapped => BestDistance < MagGraphItem.LineHeight * 0.5f;

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
    private static HashSet<MagGraphItem> CollectSnappedItems(MagGraphItem rootItem,  HashSet<MagGraphItem>? set=null)
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

    public bool IsItemDragged(MagGraphItem item) => _draggedItemIds.Contains(item.Id);

    public double LastSnapTime = double.NegativeInfinity;
    public Vector2 LastSnapPositionOnCanvas;

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
    private static HashSet<MagGraphItem> _draggedItems = [];
    private static readonly HashSet<Guid> _draggedItemIds = [];

    public Type? DraggedPrimaryOutputType;
    public Type? DraggedPrimaryInputType;
    private readonly MagGraphCanvas _canvas;
    private readonly MagGraphLayout _layout;
    private readonly NodeSelection _nodeSelection;
    private static float SnapTollerance = 0.01f;
}