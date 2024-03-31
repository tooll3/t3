using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.Graph.Modification;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.Graph.Interaction
{
    /// <summary>
    /// Handles selection and dragging (with snapping) of node canvas elements
    /// </summary>
    internal class SelectableNodeMovement(GraphWindow window, INodeCanvas canvas, NodeSelection selection)
    {
        /// <summary>
        /// Reset to avoid accidental dragging of previous elements 
        /// </summary>
        public void Reset()
        {
            _moveCommand = null;
            _draggedNodes.Clear();
        }

        /// <summary>
        /// For certain edge cases the release handling of nodes cannot be detected.
        /// This is a work around to clear the state on mouse release
        /// </summary>
        public static void CompleteFrame()
        {
            foreach (var graphWindow in GraphWindow.GraphWindowInstances)
            {
                graphWindow.GraphCanvas.SelectableNodeMovement.OnCompleteFrame();
               
            }
        }

        private void OnCompleteFrame()
        {
            if (ImGui.IsMouseReleased(0) && _moveCommand != null)
            {
                Reset();
            }
        }

        /// <summary>
        /// NOTE: This has to be called for ALL movable elements (ops, inputs, outputs) and directly after ImGui.Item
        /// </summary>
        public void Handle(ISelectableCanvasObject node, Instance instance = null)
        {

            var composition = window.CompositionOp;
            var justClicked = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup) && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
            
            var isActiveNode = node.Id == _draggedNodeId;
            if (justClicked)
            {
                _draggedNodeId = node.Id;
                var parentUi = composition.GetSymbolUi();
                if (selection.IsNodeSelected(node))
                {
                    _draggedNodes = selection.GetSelectedNodes<ISelectableCanvasObject>().ToList();
                }
                else
                {
                    if(UserSettings.Config.SmartGroupDragging)
                        _draggedNodes = FindSnappedNeighbours(node).ToList();
                    
                    _draggedNodes.Add(node);
                }

                _moveCommand = new ModifyCanvasElementsCommand(parentUi, _draggedNodes, selection);
                _shakeDetector.ResetShaking();
            }
            else if (isActiveNode && ImGui.IsMouseDown(ImGuiMouseButton.Left) && _moveCommand != null)
            {
                if (!T3Ui.IsCurrentlySaving && _shakeDetector.TestDragForShake(ImGui.GetMousePos()))
                {
                    _moveCommand.StoreCurrentValues();
                    UndoRedoStack.Add(_moveCommand);
                    DisconnectDraggedNodes();
                }
                HandleNodeDragging(node);
            }
            else if (isActiveNode && ImGui.IsMouseReleased(0) && _moveCommand != null)
            {
                if (_draggedNodeId != node.Id)
                    return;
                
                var singleDraggedNode = (_draggedNodes.Count == 1) ? _draggedNodes[0] : null;
                _draggedNodeId = Guid.Empty;
                _draggedNodes.Clear();

                var wasDragging = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).LengthSquared() > UserSettings.Config.ClickThreshold;
                if (wasDragging)
                {
                    _moveCommand.StoreCurrentValues();

                    if (singleDraggedNode != null && ConnectionSplitHelper.BestMatchLastFrame != null && singleDraggedNode is SymbolUi.Child childUi)
                    {
                        var instanceForUiChild = composition.Children[childUi.Id];
                        ConnectionMaker.SplitConnectionWithDraggedNode(window, 
                                                                       childUi, 
                                                                       ConnectionSplitHelper.BestMatchLastFrame.Connection, 
                                                                       instanceForUiChild,
                                                                       _moveCommand, selection);
                        _moveCommand = null;
                    }
                    else
                    {
                        UndoRedoStack.Add(_moveCommand);
                    }

                    // Reorder inputs nodes if dragged
                    var selectedInputs = selection.GetSelectedNodes<IInputUi>().ToList();
                    if (selectedInputs.Count > 0)
                    {
                        var compositionUi = composition.GetSymbolUi();
                        composition.Symbol.InputDefinitions.Sort((a, b) =>
                                                                 {
                                                                     var childA = compositionUi.InputUis[a.Id];
                                                                     var childB = compositionUi.InputUis[b.Id];
                                                                     return (int)(childA.PosOnCanvas.Y * 10000 + childA.PosOnCanvas.X) -
                                                                            (int)(childB.PosOnCanvas.Y * 10000 + childB.PosOnCanvas.X);
                                                                 });
                        composition.Symbol.SortInputSlotsByDefinitionOrder();
                        InputsAndOutputs.AdjustInputOrderOfSymbol(composition.Symbol);
                    }
                }
                else
                {
                    if (!selection.IsNodeSelected(node))
                    {
                        var replaceSelection = !ImGui.GetIO().KeyShift;
                        if (replaceSelection)
                        {
                            if (node is SymbolUi.Child childUi3)
                            {
                                selection.SetSelectionToChildUi(childUi3, instance);
                            }
                            else
                            {
                                selection.SetSelection(node);
                            }
                        }
                        else
                        {
                            if (node is SymbolUi.Child childUi2)
                            {
                                selection.AddSymbolChildToSelection(childUi2, instance);
                            }
                            else
                            {
                                selection.AddSelection(node);
                            }
                        }
                    }
                    else
                    {
                        if (ImGui.GetIO().KeyShift)
                        {
                            selection.DeselectNode(node, instance);
                        }
                    }
                }

                _moveCommand = null;
            }
            else if (ImGui.IsMouseReleased(0) && _moveCommand == null)
            {
                // This happens after shake
                _draggedNodes.Clear();
            }


            var wasDraggingRight = ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Length() > UserSettings.Config.ClickThreshold;
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right)
                && !wasDraggingRight
                && ImGui.IsItemHovered()
                && !selection.IsNodeSelected(node))
            {
                if (node is SymbolUi.Child childUi2)
                {
                    selection.SetSelectionToChildUi(childUi2, instance);
                }
                else
                {
                    selection.SetSelection(node);
                }
            }
        }

        private void DisconnectDraggedNodes()
        {
            var removeCommands = new List<ICommand>();
            var inputConnections = new List<(Symbol.Connection connection, Type connectionType, bool isMultiIndex, int multiInputIndex)>();
            var outputConnections = new List<(Symbol.Connection connection, Type connectionType, bool isMultiIndex, int multiInputIndex)>();
            foreach (var node in _draggedNodes)
            {
                if (node is not SymbolUi.Child childUi)
                    continue;

                if (!window.CompositionOp.Children.TryGetValue(childUi.Id, out var instance))
                {
                    Log.Error("Can't disconnect missing instance");
                    continue;
                }

                // Get all input connections and
                // relative index if they have multi-index inputs
                var connectionsToInput = instance.Parent.Symbol.Connections.FindAll(c => c.TargetParentOrChildId == instance.SymbolChildId
                                                                                   && _draggedNodes.All(c2 => c2.Id != c.SourceParentOrChildId));                
                var inConnectionInputIndex = 0;
                foreach (var connectionToInput in connectionsToInput)
                {
                    bool isMultiInput = instance.Parent.Symbol.IsTargetMultiInput(connectionToInput);
                    if (isMultiInput)
                    {
                        inConnectionInputIndex = instance.Parent.Symbol.GetMultiInputIndexFor(connectionToInput);
                    }
                    Type connectionType = instance.Inputs.Single(c => c.Id == connectionToInput.TargetSlotId).ValueType;
                    inputConnections.Add((connectionToInput, connectionType, isMultiInput, isMultiInput ? inConnectionInputIndex : 0));
                }

                // Get all output connections and
                // relative index if they have multi-index inputs
                var connectionsToOutput = instance.Parent.Symbol.Connections.FindAll(c => c.SourceParentOrChildId == instance.SymbolChildId
                                                                                    && _draggedNodes.All(c2 => c2.Id != c.TargetParentOrChildId));
                var outConnectionInputIndex = 0;
                foreach (var connectionToOutput in connectionsToOutput)
                {
                    bool isMultiInput = instance.Parent.Symbol.IsTargetMultiInput(connectionToOutput);
                    if (isMultiInput)
                    {
                        outConnectionInputIndex = instance.Parent.Symbol.GetMultiInputIndexFor(connectionToOutput);
                    }
                    Type connectionType = instance.Outputs.Single(c => c.Id == connectionToOutput.SourceSlotId).ValueType;
                    outputConnections.Add((connectionToOutput, connectionType, isMultiInput, isMultiInput ? outConnectionInputIndex : 0));
                }
            }

            // Remove the input connections in index descending order to
            // prevent to get the wrong index in case of multi-input properties
            inputConnections.Sort((x, y) => y.multiInputIndex.CompareTo(x.multiInputIndex));
            foreach (var inputConnection in inputConnections)
            {
                removeCommands.Add(new DeleteConnectionCommand(window.CompositionOp.Symbol, inputConnection.connection, inputConnection.multiInputIndex));
            }

            // Remove the output connections in index descending order to
            // prevent to get the wrong index in case of multi-input properties
            outputConnections.Sort((x, y) => y.multiInputIndex.CompareTo(x.multiInputIndex));
            foreach(var outputConnection in outputConnections)
            {
                removeCommands.Add(new DeleteConnectionCommand(window.CompositionOp.Symbol, outputConnection.connection, outputConnection.multiInputIndex));
            }

            // Reconnect inputs of 1th nodes and outputs of last nodes if are of the same type
            // and reconnect them in ascending order
            outputConnections.Sort((x, y) => x.multiInputIndex.CompareTo(y.multiInputIndex));
            inputConnections.Sort((x, y) => x.multiInputIndex.CompareTo(y.multiInputIndex));
            var outputConnectionsRemaining = new List<(Symbol.Connection connection, Type connectionType, bool isMultiIndex, int multiInputIndex)>(outputConnections);
            foreach (var itemInputConnection in inputConnections)
            {
                foreach (var itemOutputConnectionRemaining in outputConnectionsRemaining)
                {
                    if (itemInputConnection.connectionType == itemOutputConnectionRemaining.connectionType)
                    {
                        var newConnection = new Symbol.Connection(sourceParentOrChildId: itemInputConnection.connection.SourceParentOrChildId,
                                                                  sourceSlotId: itemInputConnection.connection.SourceSlotId,
                                                                  targetParentOrChildId: itemOutputConnectionRemaining.connection.TargetParentOrChildId,
                                                                  targetSlotId: itemOutputConnectionRemaining.connection.TargetSlotId);

                        removeCommands.Add(new AddConnectionCommand(window.CompositionOp.Symbol, newConnection, itemOutputConnectionRemaining.multiInputIndex));
                        outputConnectionsRemaining.Remove(itemOutputConnectionRemaining);

                        break;
                    }
                }
                if (outputConnectionsRemaining.Count < 1)
                {
                    break;
                }
            }

            if (removeCommands.Count > 0)
            {
                var macro = new MacroCommand("Shake off connections", removeCommands);
                UndoRedoStack.AddAndExecute(macro);
            }
        }

        
        private void HandleNodeDragging(ISelectableCanvasObject draggedNode)
        {
            
            if (!ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                _isDragging = false;
                return;
            }

            if (!_isDragging)
            {
                _dragStartPosInOpOnCanvas =  canvas.InverseTransformPositionFloat(ImGui.GetMousePos()) - draggedNode.PosOnCanvas;
                _isDragging = true;
            }


            var mousePosOnCanvas = canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
            var newDragPosInCanvas = mousePosOnCanvas - _dragStartPosInOpOnCanvas;

            var bestDistanceInCanvas = float.PositiveInfinity;
            var targetSnapPositionInCanvas = Vector2.Zero;

            foreach (var offset in _snapOffsetsInCanvas)
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

                foreach (var neighbor in canvas.SelectableChildren)
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

            var snapDistanceInCanvas = canvas.InverseTransformDirection(new Vector2(20, 0)).X;
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

        public void HighlightSnappedNeighbours(SymbolUi.Child childUi)
        {
            if (selection.IsNodeSelected(childUi))
                return;

            var drawList = ImGui.GetWindowDrawList();
            var parentUi = window.CompositionOp.GetSymbolUi();
            var snappedNeighbours = FindSnappedNeighbours(childUi);

            var color = new Color(1f, 1f, 1f, 0.08f);
            snappedNeighbours.Add(childUi);
            var expandOnScreen = canvas.TransformDirection(SelectableNodeMovement.SnapPadding).X / 2;

            foreach (var snappedNeighbour in snappedNeighbours)
            {
                var areaOnCanvas = new ImRect(snappedNeighbour.PosOnCanvas, snappedNeighbour.PosOnCanvas + snappedNeighbour.Size);
                var areaOnScreen = canvas.TransformRect(areaOnCanvas);
                areaOnScreen.Expand(expandOnScreen);
                drawList.AddRect(areaOnScreen.Min, areaOnScreen.Max, color, rounding: 10, ImDrawFlags.RoundCornersAll, thickness: 4);
            }
        }

        public List<ISelectableCanvasObject> FindSnappedNeighbours(ISelectableCanvasObject childUi)
        {
            //var pool = new HashSet<ISelectableNode>(parentUi.ChildUis);
            var pool = new HashSet<ISelectableCanvasObject>(canvas.SelectableChildren);
            pool.Remove(childUi);
            return RecursivelyFindSnappedInPool(childUi, pool).Distinct().ToList();
        }

        public static List<ISelectableCanvasObject> RecursivelyFindSnappedInPool(ISelectableCanvasObject childUi, HashSet<ISelectableCanvasObject> snapCandidates)
        {
            var snappedChildren = new List<ISelectableCanvasObject>();

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

        private static Alignment AreChildUisSnapped(ISelectableCanvasObject childA, ISelectableCanvasObject childB)
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

        private enum Alignment
        {
            NoSnapped,
            SnappedBelow,
            SnappedAbove,
            SnappedToRight,
            SnappedToLeft,
        }

        public static readonly Vector2 SnapPadding = new(40, 20);
        public static readonly Vector2 PaddedDefaultOpSize = SymbolUi.Child.DefaultOpSize + SnapPadding;

        private static readonly Vector2[] _snapOffsetsInCanvas =
            {
                new(SymbolUi.Child.DefaultOpSize.X + SnapPadding.X, 0),
                new(-SymbolUi.Child.DefaultOpSize.X - +SnapPadding.X, 0),
                new(0, SnapPadding.Y),
                new(0, -SnapPadding.Y)
            };

        private bool _isDragging;
        private Vector2 _dragStartPosInOpOnCanvas;

        private ModifyCanvasElementsCommand _moveCommand;
        private readonly ShakeDetector _shakeDetector = new();

        private Guid _draggedNodeId = Guid.Empty;
        private List<ISelectableCanvasObject> _draggedNodes = new();

        private class ShakeDetector
        {
            public bool TestDragForShake(Vector2 mousePosition)
            {
                var delta = mousePosition - _lastPosition;
                _lastPosition = mousePosition;

                var dx = 0;
                if (Math.Abs(delta.X) > 2)
                {
                    dx = delta.X > 0 ? 1 : -1;
                }

                _directions.Add(dx);

                if (_directions.Count < 2)
                    return false;

                // Queue length is optimized for 60 fps
                // adjust length for different frame rates
                if (_directions.Count > QueueLength * (1 / 60f) / T3.Core.Animation.Playback.LastFrameDuration)
                    _directions.RemoveAt(0);

                // count direction changes
                var changeDirectionCount = 0;

                var lastD = 0;
                var lastRealD = 0;
                foreach (var d in _directions)
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
                    ResetShaking();

                return wasShaking;
            }

            public void ResetShaking()
            {
                _directions.Clear();
            }

            private Vector2 _lastPosition = Vector2.Zero;
            private const int QueueLength = 35;
            private const int ChangeDirectionThreshold = 5;
            private readonly List<int> _directions = new(QueueLength);
        }

    }
}