using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Windows.ResearchCanvas;

/// <summary>
/// Todos:
/// - Create new connection when snapping
/// - Remove connections when unsnapping
/// - Detach block and collapse connection
/// - prevent snapping to slots with snapped connections 
///
/// Later
/// - Draw indicator for original block position (on drag start)
/// </summary>
public static class DragHandling
{
    /// <summary>
    /// NOTE: This has to be called for ALL movable elements (ops, inputs, outputs) and directly after ImGui.Item
    /// </summary>
    /// 
    public static bool HandleItemDragging(ISelectableCanvasObject node, VerticalStackingUi container, out Vector2 dragPos)
    {
        dragPos = Vector2.Zero;
        var isSnapped = false;

        var justClicked = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup) && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        var isActiveNode = node == _draggedNode;
        if (justClicked)
        {
            _draggedNode = node;
            
            
            _dampedDragPosition = node.PosOnCanvas;
            if (BlockSelection.IsNodeSelected(node))
            {
                _draggedNodes.Clear();
                _draggedNodes.UnionWith(BlockSelection.GetSelection());
            }
            else
            {
                _draggedNodes.Clear();
                _draggedNodes.Add(node);
            }
            
            _notSnappedConnectionsOnDragStart.Clear();
            foreach (var n in _draggedNodes)
            {
                if (n is not Block_Attempt1 block)
                    continue;

                foreach (var slot in block.GetSlots())
                {
                    foreach (var c in slot.Connections)
                    {
                        if (!c.IsSnapped)
                            _notSnappedConnectionsOnDragStart.Add(c);
                    }
                }
            }
        }
        else if (isActiveNode && ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            if (!ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                _isDragging = false;
            }
            else
            {
                if (!_isDragging)
                {
                    _dragStartPosInOpOnCanvas = container.Canvas.InverseTransformPositionFloat(ImGui.GetMousePos()) - node.PosOnCanvas;
                    _isDragging = true;
                }

                var mousePosOnCanvas = container.Canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
                var newDragPosInCanvas = mousePosOnCanvas - _dragStartPosInOpOnCanvas;
                node.PosOnCanvas = newDragPosInCanvas;
                dragPos = newDragPosInCanvas;
                var dragResult = SearchForSnapping(node, container, out var snapPosition);
                if (dragResult != null)
                {
                    isSnapped = dragResult.ResultType == DragResult.ResultTypes.SnappedIntoNewConnections;
                    container.Connections.AddRange(dragResult.NewConnections);
                }

                _dampedDragPosition = isSnapped
                                          ? Vector2.Lerp(_dampedDragPosition, snapPosition, 0.4f)
                                          : newDragPosInCanvas;

                var moveDeltaOnCanvas = isSnapped
                                            ? snapPosition - node.PosOnCanvas
                                            : newDragPosInCanvas - node.PosOnCanvas;

                //Log.Debug("Move delta " + moveDeltaOnCanvas + "  is snapped " + isSnapped);
                foreach (var e in _draggedNodes)
                {
                    e.PosOnCanvas += moveDeltaOnCanvas;
                }
            }
        }
        else if (isActiveNode && ImGui.IsMouseReleased(0))
        {
            if (_draggedNode != node)
                return false;

            _draggedNode = null;
            _draggedNodes.Clear();

            var wasDragging = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).LengthSquared() > UserSettings.Config.ClickThreshold;
            if (wasDragging)
            {
                // connection lines would be split here...
            }
            else
            {
                if (!BlockSelection.IsNodeSelected(node))
                {
                    var replaceSelection = !ImGui.GetIO().KeyShift;
                    if (replaceSelection)
                    {
                        BlockSelection.SetSelection(node);
                    }
                    else
                    {
                        BlockSelection.AddSelection(node);
                    }
                }
                else
                {
                    if (ImGui.GetIO().KeyShift)
                    {
                        BlockSelection.DeselectNode(node);
                    }
                }
            }
        }
        // else if (ImGui.IsMouseReleased(0)) // && _moveCommand == null)
        // {
        //     // This happens after shake
        //     _draggedNodes.Clear();
        //     _draggedNode = null;
        // }

        var wasDraggingRight = ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Length() > UserSettings.Config.ClickThreshold;
        if (ImGui.IsMouseReleased(ImGuiMouseButton.Right)
            && !wasDraggingRight
            && ImGui.IsItemHovered()
            && !BlockSelection.IsNodeSelected(node))
        {
            //BlockSelection.SetSelection(node);
        }

        return isActiveNode && isSnapped;
    }

    private static DragResult SearchForSnapping(ISelectableCanvasObject canvasObject, VerticalStackingUi container, out Vector2 delta2)
    {
        delta2 = Vector2.Zero;
        if (canvasObject is not Block_Attempt1 draggedBlock)
        {
            return null;
        }

        var foundSnapPos = false;
        var bestSnapDistance = float.PositiveInfinity;
        var bestSnapPos = Vector2.Zero;
        var newConnections = new List<Connection>();
        var bestAlreadyConnected = false;
        Slot bestSourceSlot = null;
        Slot bestTargetSlot = null;

        const int snapThreshold = 8;
        
        // Test moving block will all other blocks
        foreach (var other in container.Blocks)
        {
            if (_draggedNodes.Contains(other))
                continue;

            foreach (var output in draggedBlock.Outputs)
            {
                //snap non-connected outputs to inputs
                var verticalOutputPos = output.VerticalPosOnCanvas;
                foreach (var otherVerticalInput in other.Inputs)
                {
                    var otherVerticalInputPos = otherVerticalInput.VerticalPosOnCanvas;
                    if (!otherVerticalInput.IsConnected)
                    {
                        // free
                        BestSnapDistance(output,
                                         otherVerticalInput,
                                         verticalOutputPos,
                                         otherVerticalInputPos,
                                         other,
                                         0,
                                         false,
                                         false);
                    }
                    else if (otherVerticalInput.Connections.Count == 1 && otherVerticalInput.Connections[0].Source == output)
                    {
                        // connected to self
                        BestSnapDistance(output,
                                         otherVerticalInput,
                                         verticalOutputPos,
                                         otherVerticalInputPos,
                                         other,
                                         0,
                                         true,
                                         false);
                    }
                }

                var horizontalOutputPos = output.HorizontalPosOnCanvas;
                foreach (var otherHorizontalInput in other.Inputs)
                {
                    var otherHorizontalInputPos = otherHorizontalInput.HorizontalPosOnCanvas;
                    if (!otherHorizontalInput.IsConnected)
                    {
                        // free
                        BestSnapDistance(output,
                                         otherHorizontalInput,
                                         horizontalOutputPos,
                                         otherHorizontalInputPos,
                                         other,
                                         1,
                                         false,
                                         false);
                    }
                    else if (otherHorizontalInput.Connections.Count == 1 && otherHorizontalInput.Connections[0].Source == output)
                    {
                        // connected to self
                        BestSnapDistance(output,
                                         otherHorizontalInput,
                                         horizontalOutputPos,
                                         otherHorizontalInputPos,
                                         other,
                                         1,
                                         true,
                                         false);
                    }
                }
            }

            foreach (var input in draggedBlock.Inputs)
            {
                // snap non-connected inputs to outputs inputs
                // fixme: only if position is still free.
                var verticalInputPos = input.VerticalPosOnCanvas;
                foreach (var otherVerticalOutput in other.Outputs)
                {
                    var otherVerticalOutputPos = otherVerticalOutput.VerticalPosOnCanvas;
                    if (!otherVerticalOutput.IsConnected)
                    {
                        // free
                        BestSnapDistance(otherVerticalOutput,
                                         input,
                                         otherVerticalOutputPos,
                                         verticalInputPos,
                                         other,
                                         0,
                                         false,
                                         true);
                    }
                    else if (otherVerticalOutput.Connections.Count == 1 && otherVerticalOutput.Connections[0].Target == input)
                    {
                        // connected to self
                        BestSnapDistance(otherVerticalOutput,
                                         input,
                                         otherVerticalOutputPos,
                                         verticalInputPos,
                                         other,
                                         0,
                                         true,
                                         true);
                    }
                    else if(otherVerticalOutput.Connections.Count == 1 && otherVerticalOutput.Connections[0].GetOrientation() == Connection.Orientations.Horizontal)
                    {
                        // create additional output connection 
                        BestSnapDistance(otherVerticalOutput,
                                         input,
                                         otherVerticalOutputPos,
                                         verticalInputPos,
                                         other,
                                         0,
                                         true,
                                         true);
                    }
                }

                var horizontalInputPos = input.HorizontalPosOnCanvas;
                foreach (var otherHorizontalOutput in other.Outputs)
                {
                    var otherHorizontalOutputPos = otherHorizontalOutput.HorizontalPosOnCanvas;
                    if (!otherHorizontalOutput.IsConnected && !input.IsConnected)
                    {
                        // free
                        BestSnapDistance(otherHorizontalOutput,
                                         input,
                                         otherHorizontalOutputPos,
                                         horizontalInputPos,
                                         other,
                                         1,
                                         false,
                                         true);
                    }
                    else if (otherHorizontalOutput.Connections.Count == 1 && otherHorizontalOutput.Connections[0].Target == input)
                    {
                        // connected to self
                        BestSnapDistance(otherHorizontalOutput,
                                         input,
                                         otherHorizontalOutputPos,
                                         horizontalInputPos,
                                         other,
                                         1,
                                         true,
                                         true);
                    }
                    else if(otherHorizontalOutput.Connections.Count == 1 && otherHorizontalOutput.Connections[0].GetOrientation() == Connection.Orientations.Vertical)
                    {
                        // create additional output connection 
                        BestSnapDistance(otherHorizontalOutput,
                                         input,
                                         otherHorizontalOutputPos,
                                         horizontalInputPos,
                                         other,
                                         1,
                                         true,
                                         true);
                    }
                }
            }
        }

        if (!foundSnapPos)
        {
            if (!ImGui.GetIO().KeyShift)
            {
                foreach (var s in draggedBlock.GetSlots())
                {
                    for (var index = s.Connections.Count - 1; index >= 0; index--)
                    {
                        var c = s.Connections[index];
                        if (!c.IsSnapped && !_notSnappedConnectionsOnDragStart.Contains(c))
                        {
                            container.RemoveConnection(c);
                        }
                    }
                }
            }
            return null;
        }

        if (!bestAlreadyConnected)
            newConnections.Add(new Connection(bestSourceSlot, bestTargetSlot));

        delta2 = bestSnapPos;
        //_dampedMovePos = Vector2.Lerp(_dampedMovePos, bestSnapPos, 0.03f);
        //draggedBlock.PosOnCanvas = _dampedMovePos;
        return new DragResult()
                   {
                       NewConnections = newConnections,
                       ResultType = DragResult.ResultTypes.SnappedIntoNewConnections,
                   };


        void BestSnapDistance(Slot sourceSlot, Slot targetSlot, Vector2 sourcePos, Vector2 targetPos, Block_Attempt1 other, int anchorDirectionIndex,
                              bool alreadyConnected, bool draggedIsInput)
        {
            var delta = sourcePos - targetPos;
            var distance = delta.Length();
            if (distance > snapThreshold)
                return;

            if (!(distance < bestSnapDistance))
                return;

            bestSnapDistance = distance;
            var offset = draggedIsInput
                             ? (targetSlot.AnchorPositions[anchorDirectionIndex] - sourceSlot.AnchorPositions[anchorDirectionIndex])
                             : (sourceSlot.AnchorPositions[anchorDirectionIndex] - targetSlot.AnchorPositions[anchorDirectionIndex]);
            bestSnapPos = other.PosOnCanvas - offset *
                          VerticalStackingUi.BlockSize;

            var fixedSlot = draggedIsInput ?  sourceSlot : targetSlot;
            var pos = anchorDirectionIndex == 0 ? fixedSlot.VerticalPosOnCanvas : fixedSlot.HorizontalPosOnCanvas;
            ImGui.GetForegroundDrawList().AddCircle(container.Canvas.TransformPosition(pos), bestSnapDistance, UiColors.StatusAttention);

            bestSourceSlot = sourceSlot;
            bestTargetSlot = targetSlot;
            foundSnapPos = true;
            bestAlreadyConnected = alreadyConnected;
        }
    }

    public class DragResult
    {
        public List<Connection> NewConnections = new();
        public ResultTypes ResultType;

        public enum ResultTypes
        {
            Nothing,
            DraggedWithoutSnapping,
            SnappedIntoNewConnections,
            StillSnapped,
            Detached,
        }
    }

    private static bool _isDragging;
    private static Vector2 _dragStartPosInOpOnCanvas;

    private static HashSet<Connection> _notSnappedConnectionsOnDragStart = new();
    private static ISelectableCanvasObject _draggedNode;
    private static HashSet<ISelectableCanvasObject> _draggedNodes = new();

    private static Vector2 _dampedMovePos;
    private static Vector2 _dampedDragPosition;
}