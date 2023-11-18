using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.Graph.Modification;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.Windows.ResearchCanvas.SnapGraph;

/// <remarks>
/// Things would be slightly more efficient if this would would use SnapGraphItems. However this would
/// prevent us from reusing fence selection. Enforcing this to be used for dragging inputs and outputs
/// makes this class unnecessarily complex.
/// </remarks>
public class SnapItemMovement
{
    public SnapItemMovement(SnapGraphCanvas snapGraphCanvas, SnapGraphLayout layout)
    {
        _canvas = snapGraphCanvas;
        _layout = layout;
    }

    /// <summary>
    /// Reset to avoid accidental dragging of previous elements 
    /// </summary>
    public static void Reset()
    {
        _modifyCommand = null;
        _draggedNodes.Clear();
    }

    /// <summary>
    /// For certain edge cases the release handling of nodes cannot be detected.
    /// This is a work around to clear the state on mouse release
    /// </summary>
    public static void CompleteFrame()
    {
        if (ImGui.IsMouseReleased(0) && _modifyCommand != null)
        {
            Reset();
        }
    }

    /// <summary>
    /// NOTE: This has to be called for ALL movable elements (ops, inputs, outputs) and directly after ImGui.Item
    /// </summary>
    public void Handle(SnapGraphItem item, SnapGraphCanvas canvas)
    {
        var justClicked = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup) && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        var composition = item.Instance.Parent;
        var isActiveNode = item.Id == _draggedNodeId;
        if (justClicked)
        {
            var compositionSymbolId = composition.Symbol.Id;
            _draggedNodeId = item.Id;
            if (item.IsSelected)
            {
                _draggedNodes.Clear();
                foreach (var s in NodeSelection.Selection)
                {
                    if (_layout.Items.TryGetValue(s.Id, out var i))
                    {
                        _draggedNodes.Add(i);
                    }
                }
                //_draggedNodes = NodeSelection.GetSelectedNodes<SnapGraphItem>().ToHashSet();
            }
            else
            {
                _draggedNodes = new HashSet<SnapGraphItem> { item };
            }

            StartDragging(_draggedNodes);

            var snapGraphItems = _draggedNodes.Select(i => i as ISelectableCanvasObject).ToList();
            _modifyCommand = new ModifyCanvasElementsCommand(composition.Symbol.Id, snapGraphItems);
            //ShakeDetector.ResetShaking();
        }
        else if (isActiveNode && ImGui.IsMouseDown(ImGuiMouseButton.Left) && _modifyCommand != null)
        {
            // TODO: Implement shake disconnect later
            HandleNodeDragging(item, canvas);
        }
        else if (isActiveNode && ImGui.IsMouseReleased(0) && _modifyCommand != null)
        {
            if (_draggedNodeId != item.Id)
                return;

            var singleDraggedNode = (_draggedNodes.Count == 1) ? _draggedNodes.First() : null;
            _draggedNodeId = Guid.Empty;
            _draggedNodes.Clear();

            var wasDragging = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).LengthSquared() > UserSettings.Config.ClickThreshold;
            if (wasDragging)
            {
                _modifyCommand.StoreCurrentValues();

                if (singleDraggedNode != null
                    && ConnectionSplitHelper.BestMatchLastFrame != null
                    && singleDraggedNode is SnapGraphItem graphItem)
                {
                    //var instanceForSymbolChildUi = composition.Children.SingleOrDefault(child => child.SymbolChildId == graphItem.SymbolChildUi.Id);
                    ConnectionMaker.SplitConnectionWithDraggedNode(graphItem.SymbolChildUi,
                                                                   ConnectionSplitHelper.BestMatchLastFrame.Connection,
                                                                   graphItem.Instance,
                                                                   _modifyCommand);
                    _modifyCommand = null;
                }
                else
                {
                    UndoRedoStack.Add(_modifyCommand);
                }

                // Reorder inputs nodes if dragged
                var selectedInputs = NodeSelection.GetSelectedNodes<IInputUi>().ToList();
                if (selectedInputs.Count > 0)
                {
                    var compositionUi = SymbolUiRegistry.Entries[composition.Symbol.Id];
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
                if (!NodeSelection.IsNodeSelected(item))
                {
                    var replaceSelection = !ImGui.GetIO().KeyShift;
                    if (replaceSelection)
                    {
                        NodeSelection.SetSelectionToChildUi(item.SymbolChildUi, item.Instance);
                    }
                    else
                    {
                        NodeSelection.AddSymbolChildToSelection(item.SymbolChildUi, item.Instance);
                    }
                }
                else
                {
                    if (ImGui.GetIO().KeyShift)
                    {
                        NodeSelection.DeselectNode(item, item.Instance);
                    }
                }
            }

            _modifyCommand = null;
        }
        else if (ImGui.IsMouseReleased(0) && _modifyCommand == null)
        {
            // This happens after shake
            _draggedNodes.Clear();
        }

        var wasDraggingRight = ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Length() > UserSettings.Config.ClickThreshold;
        if (ImGui.IsMouseReleased(ImGuiMouseButton.Right)
            && !wasDraggingRight
            && ImGui.IsItemHovered()
            && !NodeSelection.IsNodeSelected(item))
        {
            NodeSelection.SetSelectionToChildUi(item.SymbolChildUi, item.Instance);
        }
    }

    private void StartDragging(HashSet<SnapGraphItem> draggedNodes)
    {
        _currentAppliedSnapOffset = Vector2.Zero;
        _lastAppliedOffset = Vector2.Zero;
        UpdateDragConnectionOnStart(draggedNodes);
    }

    private void HandleNodeDragging(ISelectableCanvasObject draggedNode, ScalableCanvas canvas)
    {
        if (!ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            _isDragging = false;
            return;
        }

        if (!_isDragging)
        {
            _dragStartPosInOpOnCanvas = canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
            _isDragging = true;
        }

        var dl = ImGui.GetWindowDrawList();
        var showDebug = ImGui.GetIO().KeyCtrl;
        var mousePosOnCanvas = canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
        var requestedDeltaOnCanvas = mousePosOnCanvas - _dragStartPosInOpOnCanvas;

        var dragExtend = SnapGraphItem.GetGroupBoundingBox(_draggedNodes);
        dragExtend.Expand(SnapThreshold * canvas.Scale.X);

        if (showDebug)
        {
            dl.AddCircle(_canvas.TransformPosition(_dragStartPosInOpOnCanvas), 10, Color.Blue);

            dl.AddLine(_canvas.TransformPosition(_dragStartPosInOpOnCanvas),
                       _canvas.TransformPosition(_dragStartPosInOpOnCanvas + requestedDeltaOnCanvas), Color.Blue);

            dl.AddRect(_canvas.TransformPosition(dragExtend.Min),
                       _canvas.TransformPosition(dragExtend.Max),
                       Color.Green.Fade(0.1f));
        }

        var overlappingItems = new List<SnapGraphItem>();
        foreach (var otherItem in _layout.Items.Values)
        {
            if (_draggedNodes.Contains(otherItem) || !dragExtend.Overlaps(otherItem.Area))
                continue;

            overlappingItems.Add(otherItem);
        }

        var bestSnapDistance = float.PositiveInfinity;
        Vector2 bestSnapDelta = default;

        // New possible ConnectionsOptions
        List<Symbol.Connection> newPossibleConnections = new();

        // Move back to non-snapped position
        foreach (var n in _draggedNodes)
        {
            n.PosOnCanvas -= _lastAppliedOffset; // Move to position
            n.PosOnCanvas += requestedDeltaOnCanvas; // Move to request position
        }

        _lastAppliedOffset = requestedDeltaOnCanvas;

        // Yes, that code looks weird.
        foreach (var otherItem in overlappingItems)
        {
            foreach (var draggedItem in _draggedNodes)
            {
                foreach (var draggedInAnchor in draggedItem.GetInputAnchors())
                {
                    foreach (var otherOutAnchor in otherItem.GetOutputAnchors())
                    {
                        var d = otherOutAnchor.GetSnapDistance(draggedInAnchor);
                        if (showDebug)
                            dl.AddLine(_canvas.TransformPosition(otherOutAnchor.PositionOnCanvas),
                                       _canvas.TransformPosition(draggedInAnchor.PositionOnCanvas),
                                       Color.Red.Fade(d > 10000 ? 0.1f : 1));

                        if (d > bestSnapDistance)
                            continue;

                        if (d < bestSnapDistance)
                            newPossibleConnections.Clear();

                        if (!otherOutAnchor.IsConnected)
                            newPossibleConnections.Add(new Symbol.Connection(
                                                                             sourceParentOrChildId: otherItem.Id,
                                                                             sourceSlotId: otherOutAnchor.SlotId,
                                                                             targetParentOrChildId: draggedItem.Id,
                                                                             targetSlotId: draggedInAnchor.SlotId)
                                                      );

                        bestSnapDelta = otherOutAnchor.PositionOnCanvas - draggedInAnchor.PositionOnCanvas;
                        bestSnapDistance = d;
                    }
                }

                foreach (var draggedOutAnchor in draggedItem.GetOutputAnchors())
                {
                    foreach (var otherInAnchor in otherItem.GetInputAnchors())
                    {
                        var d = otherInAnchor.GetSnapDistance(draggedOutAnchor);
                        if (showDebug)
                            dl.AddLine(_canvas.TransformPosition(draggedOutAnchor.PositionOnCanvas),
                                       _canvas.TransformPosition(otherInAnchor.PositionOnCanvas),
                                       Color.Green.Fade(d > 10000 ? 0.1f : 1));

                        if (d > bestSnapDistance)
                            continue;

                        if (d < bestSnapDistance)
                            newPossibleConnections.Clear();

                        if (!otherInAnchor.IsConnected)
                            newPossibleConnections.Add(new Symbol.Connection(
                                                                             sourceParentOrChildId: draggedItem.Id,
                                                                             sourceSlotId: draggedOutAnchor.SlotId,
                                                                             targetParentOrChildId: otherItem.Id,
                                                                             targetSlotId: otherInAnchor.SlotId));

                        bestSnapDelta = otherInAnchor.PositionOnCanvas - draggedOutAnchor.PositionOnCanvas;
                        bestSnapDistance = d;
                    }
                }
            }
        }

        // Snapped
        if (bestSnapDistance < SnapGraphItem.LineHeight * 0.5f)
        {
            dl.AddLine(_canvas.TransformPosition(mousePosOnCanvas),
                       canvas.TransformPosition(mousePosOnCanvas) + _canvas.TransformDirection(bestSnapDelta),
                       Color.White);

            foreach (var n in _draggedNodes)
            {
                n.PosOnCanvas += bestSnapDelta;
            }

            _lastAppliedOffset += bestSnapDelta;
        }
    }

    private void UpdateDragConnectionOnStart(HashSet<SnapGraphItem> draggedItems)
    {
        _bridgeConnectionsOnStart.Clear();
        foreach (var c in _layout.SnapConnections)
        {
            var targetDragged = draggedItems.Contains(c.TargetItem);
            var sourceDragged = draggedItems.Contains(c.TargetItem);
            if (targetDragged != sourceDragged)
            {
                _bridgeConnectionsOnStart.Add(c);
            }
        }
    }

    private Vector2 _currentAppliedSnapOffset;
    private Vector2 _lastAppliedOffset;

    private const float SnapThreshold = 10;
    private readonly List<SnapGraphConnection> _bridgeConnectionsOnStart = new();

    private static bool _isDragging;
    private static Vector2 _dragStartPosInOpOnCanvas;

    private static ModifyCanvasElementsCommand _modifyCommand;

    private static Guid _draggedNodeId = Guid.Empty;
    private static HashSet<SnapGraphItem> _draggedNodes = new();
    private readonly SnapGraphCanvas _canvas;
    private readonly SnapGraphLayout _layout;
}