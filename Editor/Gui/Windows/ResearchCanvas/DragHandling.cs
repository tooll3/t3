using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
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
    public static bool HandleItemDragging(ISelectableCanvasObject node, VerticalStackingCanvas container,  out Vector2 dragPos)
    {
        dragPos = Vector2.Zero;
        var isSnapped = false;
        
        var justClicked = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup) && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        var isActiveNode = node == _draggedNode;
        if (justClicked)
        {
            _draggedNode = node;
            _dampedDragPosition = node.PosOnCanvas;
            if (node.IsSelected)
            {
                _draggedNodes = BlockSelection.SelectedNodes;
            }
            else
            {
                _draggedNodes.Add(node);
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
                    isSnapped = dragResult.ResultType ==  DragResult.ResultTypes.SnappedIntoNewConnections;
                    container.Connections.AddRange(dragResult.NewConnections);
                    
                }
                
                _dampedDragPosition = isSnapped
                                          ? Vector2.Lerp(_dampedDragPosition, snapPosition, 0.6f)
                                          : newDragPosInCanvas;

                var moveDeltaOnCanvas = isSnapped
                                            ? _dampedDragPosition - node.PosOnCanvas
                                            : newDragPosInCanvas - node.PosOnCanvas;

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
            BlockSelection.SetSelection(node);
        }

        return isActiveNode && isSnapped;
    }



    private static DragResult SearchForSnapping(ISelectableCanvasObject canvasObject, VerticalStackingCanvas container, out Vector2 delta2)
    {
        delta2 = Vector2.Zero;
        if (canvasObject is not Block movingTestBlock)
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
            
        
        var snapThreshold = 20;

        // connect to outer slots
        foreach (var movingBlockSlot in movingTestBlock.GetSlots())
        {
            
            var slotPosA = movingBlockSlot.Block.PosOnCanvas + movingBlockSlot.AnchorPos * VerticalStackingCanvas.BlockSize;
            var isSlotHorizontal = Math.Abs(movingBlockSlot.AnchorPos.Y - 0.5f) < 0.001f;

            foreach (var other in container.Blocks)
            {
                
                if (other == movingTestBlock)
                    continue;

                var otherSlots = movingBlockSlot.IsInput ? other.Outputs : other.Inputs;
                foreach (var otherSlot in otherSlots)
                {
                    var isLinking = false;
                    if (movingBlockSlot.Connections.Count == 1)
                    {
                        //var connection = movingBlockSlot.Connections[0];
                        isLinking = movingBlockSlot.Connections[0].IsConnecting(movingBlockSlot, otherSlot);
                        if (!isLinking)
                            continue;
                    }                
                    
                    var isOtherSlotHorizontal = Math.Abs(otherSlot.AnchorPos.Y - 0.5f) < 0.001f;
                    if (isSlotHorizontal != isOtherSlotHorizontal)
                        continue;
                    
                    
                    var otherSlotPos = other.PosOnCanvas + otherSlot.AnchorPos * VerticalStackingCanvas.BlockSize;
                    var delta = slotPosA - otherSlotPos;
                    var distance = delta.Length();
                    if (distance > snapThreshold)
                        continue;

                    if (distance < bestSnapDistance)
                    {
                        bestSnapDistance = distance;
                        bestSnapPos = other.PosOnCanvas - (movingBlockSlot.AnchorPos - otherSlot.AnchorPos) * VerticalStackingCanvas.BlockSize;
                        ImGui.GetForegroundDrawList().AddCircle(container.Canvas.TransformPosition(otherSlot.PosOnCanvas), bestSnapDistance, UiColors.StatusAttention);

                        // Fix me: This could multiple pairs later...
                        bestSourceSlot = movingBlockSlot.IsInput ? otherSlot : movingBlockSlot;
                        bestTargetSlot = movingBlockSlot.IsInput ? movingBlockSlot : otherSlot;
                        foundSnapPos = true;
                        bestAlreadyConnected = isLinking;
                    }
                }
            }
            
            // Split inner connections
            // ...
        }

        if (!foundSnapPos)
            return null;
        
        if(!bestAlreadyConnected)
            newConnections.Add(new Connection(bestSourceSlot, bestTargetSlot));
        
        delta2 = bestSnapPos;
        _dampedMovePos = Vector2.Lerp(_dampedMovePos, bestSnapPos, 0.5f);
        movingTestBlock.PosOnCanvas = _dampedMovePos;
        return new DragResult()
                   {
                       NewConnections = newConnections,
                       ResultType = DragResult.ResultTypes.SnappedIntoNewConnections,
                   };
    }
    
    public class DragResult
    {
        public List<Connection> NewConnections = new();
        public ResultTypes ResultType;
        
        public enum ResultTypes {
            Nothing,
            DraggedWithoutSnapping,
            SnappedIntoNewConnections,
            StillSnapped,
            Detached,
        }
    }

    private static bool _isDragging;
    private static Vector2 _dragStartPosInOpOnCanvas;
    
    private static ISelectableCanvasObject _draggedNode;
    private static HashSet<ISelectableCanvasObject> _draggedNodes = new();
    
    private static Vector2 _dampedMovePos;
    private static Vector2 _dampedDragPosition;
}