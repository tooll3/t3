using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.InputUi;
using T3.Gui.Selection;
using UiHelpers;
using Vector2 = System.Numerics.Vector2;

namespace T3.Gui.Graph.Interaction
{
    /// <summary>
    /// Handles selection and dragging (with snapping) of node canvas elements
    /// </summary>
    public static class SelectableNodeMovement
    {
        /// <summary>
        /// NOTE: This has to be called directly after ImGui.Item
        /// </summary>
        public static void Handle(ISelectableNode node, Instance instance = null)
        {
            if (ImGui.IsItemActive())
            {
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    var compositionSymbolId = GraphCanvas.Current.CompositionOp.Symbol.Id;
                    _draggedNodeId = node.Id;
                    if (node.IsSelected)
                    {
                        _draggedNodes = SelectionManager.GetSelectedNodes<ISelectableNode>().ToList();
                    }
                    else
                    {
                        var parentUi = SymbolUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
                        _draggedNodes = FindSnappedNeighbours(parentUi, node).ToList();
                        _draggedNodes.Add(node);
                    }

                    _moveCommand = new ChangeSelectableCommand(compositionSymbolId, _draggedNodes);
                    ShakeDetector.Reset();
                }
                else if (_moveCommand != null)
                {
                    if (ShakeDetector.TestDragForShake(ImGui.GetMousePos()))
                    {
                        _moveCommand.StoreCurrentValues();
                        UndoRedoStack.Add(_moveCommand);
                        _moveCommand = null;
                        DisconnectDraggedNodes();
                    }
                }

                HandleNodeDragging(node);
            }
            else if (ImGui.IsMouseReleased(0) && _moveCommand != null)
            {
                if (_draggedNodeId != node.Id)
                    return;

                var singleDraggedNode = (_draggedNodes.Count == 1) ? _draggedNodes[0] : null;
                _draggedNodeId = Guid.Empty;
                _draggedNodes.Clear();

                var wasDragging = ImGui.GetMouseDragDelta(0).LengthSquared() > 0.0f;
                if (wasDragging)
                {
                    _moveCommand.StoreCurrentValues();
                    UndoRedoStack.Add(_moveCommand);

                    if (singleDraggedNode != null && ConnectionMaker.ConnectionSplitHelper.BestMatchLastFrame != null && singleDraggedNode is SymbolChildUi childUi)
                    {
                        ConnectionMaker.SplitConnectionWithDraggedNode(ConnectionMaker.ConnectionSplitHelper.BestMatchLastFrame.Connection, 
                                                                       SelectionManager.GetInstanceForSymbolChildUi(childUi)
                                                                       );
                    }

                    // Reorder inputs nodes if dragged
                    var selectedInputs = SelectionManager.GetSelectedNodes<IInputUi>().ToList();
                    if (selectedInputs.Count > 0)
                    {
                        var composition = GraphCanvas.Current.CompositionOp;
                        var compositionUi = SymbolUiRegistry.Entries[composition.Symbol.Id];
                        composition.Symbol.InputDefinitions.Sort((a, b) =>
                                                                 {
                                                                     var childA = compositionUi.InputUis[a.Id];
                                                                     var childB = compositionUi.InputUis[b.Id];
                                                                     return (int)(childA.PosOnCanvas.Y * 10000 + childA.PosOnCanvas.X) -
                                                                            (int)(childB.PosOnCanvas.Y * 10000 + childB.PosOnCanvas.X);
                                                                 });
                        composition.Symbol.SortInputSlotsByDefinitionOrder();
                        NodeOperations.AdjustInputOrderOfSymbol(composition.Symbol);
                    }
                }
                else
                {
                    if (!SelectionManager.IsNodeSelected(node))
                    {
                        if (!ImGui.GetIO().KeyShift)
                        {
                            SelectionManager.Clear();
                        }

                        if (node is SymbolChildUi childUi2)
                        {
                            SelectionManager.AddSelection(childUi2, instance);
                        }
                        else
                        {
                            SelectionManager.AddSelection(node);
                        }
                    }
                    else
                    {
                        if (ImGui.GetIO().KeyShift)
                        {
                            SelectionManager.RemoveSelection(node);
                        }
                    }
                }

                _moveCommand = null;
            }

            var wasDraggingRight = ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).LengthSquared() > 0.0f;
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right)
                && !wasDraggingRight
                && ImGui.IsItemHovered()
                && !SelectionManager.IsNodeSelected(node))
            {
                if (node is SymbolChildUi childUi2)
                {
                    SelectionManager.SetSelection(childUi2, instance);
                }
                else
                {
                    SelectionManager.SetSelection(node);
                }
            }
        }

        private static void DisconnectDraggedNodes()
        {
            var removeCommands = new List<ICommand>();
            var inputConnections = new List<Symbol.Connection>();
            var outputConnections = new List<Symbol.Connection>();
            Type inputConnectionType = null;
            Type outputConnectionType = null;

            foreach (var nod in _draggedNodes)
            {
                if (!(nod is SymbolChildUi childUi))
                    continue;

                var instance = GraphCanvas.Current.CompositionOp.Children.Single(child => child.SymbolChildId == childUi.Id);

                foreach (var input in instance.Inputs)
                {
                    if (!input.IsConnected)
                        continue;

                    var connectionsToInput = instance.Parent.Symbol.Connections.FindAll(c => c.TargetParentOrChildId == instance.SymbolChildId
                                                                                             && _draggedNodes.All(c2 => c2.Id != c.SourceParentOrChildId));
                    var lastTargetId = Guid.Empty;
                    var lastInputId = Guid.Empty;
                    var multiInputSlotIndex = 0;
                    foreach (var connectionToInput in connectionsToInput)
                    {
                        removeCommands.Add(new DeleteConnectionCommand(GraphCanvas.Current.CompositionOp.Symbol, connectionToInput, multiInputSlotIndex));
                        inputConnections.Add(connectionToInput);
                        inputConnectionType = input.ValueType;
                        if (connectionToInput.TargetParentOrChildId == lastTargetId
                            && connectionToInput.TargetSlotId == lastInputId)
                        {
                            multiInputSlotIndex++;
                        }
                        else
                        {
                            multiInputSlotIndex = 0;
                        }
                    }
                }

                foreach (var output in instance.Outputs)
                {
                    var connectionsToOutput =
                        instance.Parent.Symbol.Connections.FindAll(c => c.SourceParentOrChildId == instance.SymbolChildId
                                                                        && _draggedNodes.All(c2 => c2.Id != c.TargetParentOrChildId));
                    foreach (var outputConnection in connectionsToOutput)
                    {
                        removeCommands.Add(new DeleteConnectionCommand(GraphCanvas.Current.CompositionOp.Symbol, outputConnection, 0));
                        outputConnections.Add(outputConnection);
                        outputConnectionType = output.ValueType;
                    }
                }
            }

            if (inputConnections.Count == 1
                && outputConnections.Count == 1
                && inputConnectionType == outputConnectionType
                )
            {
                var newConnection = new Symbol.Connection(sourceParentOrChildId: inputConnections[0].SourceParentOrChildId,
                                                          sourceSlotId: inputConnections[0].SourceSlotId,
                                                          targetParentOrChildId: outputConnections[0].TargetParentOrChildId,
                                                          targetSlotId: outputConnections[0].TargetSlotId);
                removeCommands.Add(new AddConnectionCommand(GraphCanvas.Current.CompositionOp.Symbol, newConnection, 0));
            }

            if (removeCommands.Count > 0)
            {
                var macro = new MacroCommand("Shake off connections", removeCommands);
                UndoRedoStack.AddAndExecute(macro);
            }
        }

        private static void HandleNodeDragging(ISelectableNode draggedNode)
        {
            if (!ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                _isDragging = false;
                return;
            }

            if (!_isDragging)
            {
                _dragStartDelta = ImGui.GetMousePos() - GraphCanvas.Current.TransformPosition(draggedNode.PosOnCanvas);
                _isDragging = true;
            }

            var newDragPos = ImGui.GetMousePos() - _dragStartDelta;
            var newDragPosInCanvas = GraphCanvas.Current.InverseTransformPosition(newDragPos);

            var bestDistanceInCanvas = float.PositiveInfinity;
            var targetSnapPositionInCanvas = Vector2.Zero;

            foreach (var offset in SnapOffsetsInCanvas)
            {
                var heightAffectFactor = 0;
                if (Math.Abs(offset.X) < 0.01f)
                {
                    if (offset.Y > 0)
                    {
                        heightAffectFactor = -1;
                    }
                    else
                    {
                        heightAffectFactor = 1;
                    }
                }

                foreach (var neighbor in GraphCanvas.Current.SelectableChildren)
                {
                    if (neighbor == draggedNode || _draggedNodes.Contains(neighbor))
                        continue;

                    var offset2 = new Vector2(offset.X, -neighbor.Size.Y * heightAffectFactor + offset.Y);
                    var snapToNeighborPos = neighbor.PosOnCanvas + offset2;

                    var d = Vector2.Distance(snapToNeighborPos, newDragPosInCanvas);
                    if (!(d < bestDistanceInCanvas))
                        continue;

                    targetSnapPositionInCanvas = snapToNeighborPos;
                    bestDistanceInCanvas = d;
                }
            }

            var snapDistanceInCanvas = GraphCanvas.Current.InverseTransformDirection(new Vector2(20, 0)).X;
            var isSnapping = bestDistanceInCanvas < snapDistanceInCanvas;

            var moveDeltaOnCanvas = isSnapping
                                        ? targetSnapPositionInCanvas - draggedNode.PosOnCanvas
                                        : newDragPosInCanvas - draggedNode.PosOnCanvas;

            // Drag selection
            foreach (var e in _draggedNodes)
            {
                e.PosOnCanvas += moveDeltaOnCanvas;
            }
        }

        public static void HighlightSnappedNeighbours(SymbolChildUi childUi)
        {
            if (childUi.IsSelected)
                return;

            var drawList = ImGui.GetWindowDrawList();
            var parentUi = SymbolUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
            var snappedNeighbours = FindSnappedNeighbours(parentUi, childUi);

            var color = new Color(1f, 1f, 1f, 0.08f);
            snappedNeighbours.Add(childUi);
            var expandOnScreen = GraphCanvas.Current.TransformDirection(SelectableNodeMovement.SnapPadding).X / 2;

            foreach (var snappedNeighbour in snappedNeighbours)
            {
                var areaOnCanvas = new ImRect(snappedNeighbour.PosOnCanvas, snappedNeighbour.PosOnCanvas + snappedNeighbour.Size);
                var areaOnScreen = GraphCanvas.Current.TransformRect(areaOnCanvas);
                areaOnScreen.Expand(expandOnScreen);
                drawList.AddRect(areaOnScreen.Min, areaOnScreen.Max, color, rounding: 10, rounding_corners: ImDrawCornerFlags.All, thickness: 4);
            }
        }

        public static List<ISelectableNode> FindSnappedNeighbours(SymbolUi parentUi, ISelectableNode childUi)
        {
            //var pool = new HashSet<ISelectableNode>(parentUi.ChildUis);
            var pool = new HashSet<ISelectableNode>(GraphCanvas.Current.SelectableChildren);
            pool.Remove(childUi);
            return RecursivelyFindSnappedInPool(childUi, pool).Distinct().ToList();
        }

        public static List<ISelectableNode> RecursivelyFindSnappedInPool(ISelectableNode childUi, HashSet<ISelectableNode> snapCandidates)
        {
            var snappedChildren = new List<ISelectableNode>();

            foreach (var candidate in snapCandidates.ToList())
            {
                if (AreChildUisSnapped(childUi, candidate) == Alignment.NoSnapped)
                    continue;

                snappedChildren.Add(candidate);
                snapCandidates.Remove(candidate);
                snappedChildren.AddRange(RecursivelyFindSnappedInPool(candidate, snapCandidates));
            }

            return snappedChildren;
        }

        public const float Tolerance = 0.1f;

        private static Alignment AreChildUisSnapped(ISelectableNode childA, ISelectableNode childB)
        {
            var alignedHorizontally = Math.Abs(childA.PosOnCanvas.Y - childB.PosOnCanvas.Y) < Tolerance;
            var alignedVertically = Math.Abs(childA.PosOnCanvas.X - childB.PosOnCanvas.X) < Tolerance;

            if (alignedVertically)
            {
                if (Math.Abs(childA.PosOnCanvas.Y + childA.Size.Y + SelectableNodeMovement.SnapPadding.Y
                             - childB.PosOnCanvas.Y) < Tolerance)
                {
                    return Alignment.SnappedBelow;
                }

                if (Math.Abs(childB.PosOnCanvas.Y + childB.Size.Y + SelectableNodeMovement.SnapPadding.Y
                             - childA.PosOnCanvas.Y) < Tolerance)
                {
                    return Alignment.SnappedAbove;
                }
            }

            if (alignedHorizontally)
            {
                if (Math.Abs(childA.PosOnCanvas.X + childA.Size.X + SelectableNodeMovement.SnapPadding.X
                             - childB.PosOnCanvas.X) < Tolerance)
                {
                    return Alignment.SnappedToRight;
                }

                if (Math.Abs(childB.PosOnCanvas.X + childB.Size.X + SelectableNodeMovement.SnapPadding.X
                             - childA.PosOnCanvas.X) < Tolerance)
                {
                    return Alignment.SnappedToLeft;
                }
            }

            return Alignment.NoSnapped;
        }

        // private static double RecursivelyAlignChildrenOfSelectedOps(SymbolChildUi childUi, List<ISelectableNode> freshlySnappedOpWidgets)
        // {
        //     var connectedInstances = GetConnectedInstances(childUi);
        //     var parentUi = SymbolUiRegistry.Entries[GraphCanvas.Current.CompositionOp.SymbolChildId];
        //     var connectedChildUis = connectedInstances
        //                            .Select(ci => parentUi.ChildUis.Single(childUi2 => childUi2.Id == ci.SymbolChildId))
        //                            .ToList();
        //     
        //     var compositionSymbol = GraphCanvas.Current.CompositionOp.Symbol;
        //     
        //     // var childUis = (from con in compositionSymbol.Connections
        //     //                 where !con.IsConnectedToSymbolInput && !con.IsConnectedToSymbolOutput
        //     //                 from childUi in compositionSymbolUi.ChildUis
        //     //                 where con.SourceParentOrChildId == childUi.Id
        //     //                 select childUi).Distinct();            
        //
        //     foreach (var connectedChild in connectedChildUis)
        //     {
        //         if (childUi.IsSelected)
        //             continue;
        //         
        //         
        //     }
        //     return 0;
        // }

        // private static List<Instance> GetConnectedInstances(SymbolChildUi childUi)
        // {
        //     var instance = GraphCanvas.Current.CompositionOp.Children.Single(child => child.SymbolChildId == childUi.Id);
        //
        //     var connectedInstances = new List<Instance>();
        //     foreach (var i in instance.Inputs)
        //     {
        //         if (!i.IsConnected)
        //             continue;
        //
        //         var connection = instance.Parent.Symbol.Connections.Single(c => c.TargetParentOrChildId == instance.SymbolChildId);
        //         connectedInstances.Add(instance.Parent.Children.Single(child => child.SymbolChildId == connection.SourceParentOrChildId));
        //     }
        //
        //     return connectedInstances;
        // }

        private enum Alignment
        {
            NoSnapped,
            SnappedBelow,
            SnappedAbove,
            SnappedToRight,
            SnappedToLeft,
        }

        public static readonly Vector2 SnapPadding = new Vector2(20, 20);

        private static readonly Vector2[] SnapOffsetsInCanvas =
            {
                new Vector2(SymbolChildUi.DefaultOpSize.X + SnapPadding.X, 0),
                new Vector2(-SymbolChildUi.DefaultOpSize.X - +SnapPadding.X, 0),
                new Vector2(0, SnapPadding.Y),
                new Vector2(0, -SnapPadding.Y)
            };

        private static bool _isDragging;
        private static Vector2 _dragStartDelta;
        private static ChangeSelectableCommand _moveCommand;

        private static Guid _draggedNodeId = Guid.Empty;
        private static List<ISelectableNode> _draggedNodes = new List<ISelectableNode>();

        private static class ShakeDetector
        {
            public static bool TestDragForShake(Vector2 mousePosition)
            {
                var delta = mousePosition - _lastPosition;
                _lastPosition = mousePosition;

                int dx = 0;
                if (Math.Abs(delta.X) > 2)
                {
                    dx = delta.X > 0 ? 1 : -1;
                }

                Directions.Add(dx);

                if (Directions.Count < 2)
                    return false;

                if (Directions.Count > QueueLength)
                    Directions.RemoveAt(0);

                // count direction changes
                var changeDirectionCount = 0;

                var lastD = 0;
                var lastRealD = 0;
                foreach (var d in Directions)
                {
                    if (lastD != 0 && d != 0)
                    {
                        if (d != lastRealD)
                        {
                            changeDirectionCount++;
                        }

                        lastRealD = d;
                    }

                    lastD = d;
                }

                var wasShaking = changeDirectionCount >= ChangeDirectionThreshold;
                if (wasShaking)
                    Reset();

                return wasShaking;
            }

            public static void Reset()
            {
                Directions.Clear();
            }

            private static Vector2 _lastPosition = Vector2.Zero;
            private const int QueueLength = 25;
            private const int ChangeDirectionThreshold = 5;
            private static readonly List<int> Directions = new List<int>(QueueLength);
        }
    }
}