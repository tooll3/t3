using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Windows.ResearchCanvas;

public static class DragHandling
{
    public delegate bool SnapHandler(ISelectableCanvasObject canvasObject, out Vector2 delta2);

    private static Vector2 _dampedDragPosition;

    /// <summary>
    /// NOTE: This has to be called for ALL movable elements (ops, inputs, outputs) and directly after ImGui.Item
    /// </summary>
    /// 
    public static void HandleItemDragging(ISelectableCanvasObject node, ScalableCanvas canvas, SnapHandler snapTest)
    {
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
                    _dragStartPosInOpOnCanvas = canvas.InverseTransformPositionFloat(ImGui.GetMousePos()) - node.PosOnCanvas;
                    _isDragging = true;
                }

                var mousePosOnCanvas = canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
                var newDragPosInCanvas = mousePosOnCanvas - _dragStartPosInOpOnCanvas;
                node.PosOnCanvas = newDragPosInCanvas;

                var isSnapped = snapTest(node, out var snapPosition);

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
                return;

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

    private static bool _isDragging;
    private static Vector2 _dragStartPosInOpOnCanvas;

    private static readonly HashSet<ISelectableCanvasObject> _selectedNodes = new();

    private static ISelectableCanvasObject _draggedNode;
    private static HashSet<ISelectableCanvasObject> _draggedNodes = new();
}