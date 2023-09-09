using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Selection;
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
                _draggedNodes = _selectedNodes;
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
                isSnapped = SearchForSnapping(node, container.Blocks, out var snapPosition);

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
                if (!IsNodeSelected(node))
                {
                    var replaceSelection = !ImGui.GetIO().KeyShift;
                    if (replaceSelection)
                    {
                        SetSelection(node);
                    }
                    else
                    {
                        AddSelection(node);
                    }
                }
                else
                {
                    if (ImGui.GetIO().KeyShift)
                    {
                        DeselectNode(node);
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
            && !IsNodeSelected(node))
        {
            SetSelection(node);
        }

        return isActiveNode && isSnapped;
    }

    public static void SetSelection(ISelectableCanvasObject selectedObject)
    {
        _selectedNodes.Clear();
        _selectedNodes.Add(selectedObject);
    }

    public static bool IsNodeSelected(ISelectableCanvasObject node)
    {
        return _selectedNodes.Contains(node);
    }

    public static void AddSelection(IEnumerable<ISelectableCanvasObject> additionalObjects)
    {
        _selectedNodes.UnionWith(additionalObjects);
    }

    public static void AddSelection(ISelectableCanvasObject additionalObject)
    {
        _selectedNodes.Add(additionalObject);
    }

    public static void DeselectNode(ISelectableCanvasObject objectToRemove)
    {
        _selectedNodes.Remove(objectToRemove);
    }

    public class DragResult
    {
        private List<Connection> NewConnections;
        
        public enum ResultType {
            Nothing,
            DraggedWithoutSnapping,
            SnappedIntoNewConnections,
            StillSnapped,
            Detached,
        }
    }

    private static bool _isDragging;
    private static Vector2 _dragStartPosInOpOnCanvas;

    private static readonly HashSet<ISelectableCanvasObject> _selectedNodes = new();

    private static ISelectableCanvasObject _draggedNode;
    private static HashSet<ISelectableCanvasObject> _draggedNodes = new();

    public static bool SearchForSnapping(ISelectableCanvasObject canvasObject, List<Block> blocks, out Vector2 delta2)
    {
        delta2 = Vector2.Zero;
        if (canvasObject is not Block movingTestBlock)
        {
            return false;
        }
            
        var foundSnapPos = false;
        var bestSnapDistance = float.PositiveInfinity;
        var bestSnapPos = Vector2.Zero;
        var snapThreshold = 20;

        foreach (var movingBlockSlot in movingTestBlock.GetSlots())
        {
            var slotPosA = movingBlockSlot.Block.PosOnCanvas + movingBlockSlot.AnchorPos * VerticalStackingCanvas.BlockSize;
            var isSlotHorizontal = Math.Abs(movingBlockSlot.AnchorPos.Y - 0.5f) < 0.001f;

            foreach (var other in blocks)
            {
                if (other == movingTestBlock)
                    continue;

                var otherSlots = movingBlockSlot.IsInput ? other.Outputs : other.Inputs;
                foreach (var otherSlot in otherSlots)
                {
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
                        foundSnapPos = true;
                    }
                }
            }
        }

        if (!foundSnapPos)
            return false;
        
        delta2 = bestSnapPos;
        _dampedMovePos = Vector2.Lerp(_dampedMovePos, bestSnapPos, 0.5f);
        movingTestBlock.PosOnCanvas = _dampedMovePos;
        return true;
    }
    
    private static Vector2 _dampedMovePos;
    private static Vector2 _dampedDragPosition;
}