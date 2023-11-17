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
                _draggedNodes = NodeSelection.GetSelectedNodes<ISelectableCanvasObject>().ToList();
            }
            else
            {
                _draggedNodes = new List<ISelectableCanvasObject> { item.SymbolChildUi };
            }

            StartDragging(_draggedNodes);
            
            _modifyCommand = new ModifyCanvasElementsCommand(compositionSymbolId, _draggedNodes);
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
                
            var singleDraggedNode = (_draggedNodes.Count == 1) ? _draggedNodes[0] : null;
            _draggedNodeId = Guid.Empty;
            _draggedNodes.Clear();

            var wasDragging = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).LengthSquared() > UserSettings.Config.ClickThreshold;
            if (wasDragging)
            {
                _modifyCommand.StoreCurrentValues();

                if (singleDraggedNode != null && ConnectionSplitHelper.BestMatchLastFrame != null && singleDraggedNode is SymbolChildUi childUi)
                {
                    var instanceForSymbolChildUi = composition.Children.SingleOrDefault(child => child.SymbolChildId == childUi.Id);
                    ConnectionMaker.SplitConnectionWithDraggedNode(childUi, 
                                                                   ConnectionSplitHelper.BestMatchLastFrame.Connection, 
                                                                   instanceForSymbolChildUi,
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

    private static void StartDragging(List<ISelectableCanvasObject> draggedNodes)
    {
        foreach (var t in draggedNodes)
        {
            
        }
        
        
    }

    private static void HandleNodeDragging(ISelectableCanvasObject draggedNode, ScalableCanvas canvas)
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

        // var bestDistanceInCanvas = float.PositiveInfinity;
        // var targetSnapPositionInCanvas = Vector2.Zero;
        //
        // foreach (var offset in _snapOffsetsInCanvas)
        // {
        //     var heightAffectFactor = 0;
        //     if (Math.Abs(offset.X) < 0.01f)
        //     {
        //         if (offset.Y > 0)
        //         {
        //             heightAffectFactor = -1;
        //         }
        //         else
        //         {
        //             heightAffectFactor = 1;
        //         }
        //     }
        //
        //     foreach (var neighbor in GraphCanvas.Current.SelectableChildren)
        //     {
        //         if (neighbor == draggedNode || _draggedNodes.Contains(neighbor))
        //             continue;
        //
        //         var offset2 = new Vector2(offset.X, -neighbor.Size.Y * heightAffectFactor + offset.Y);
        //         var snapToNeighborPos = neighbor.PosOnCanvas + offset2;
        //
        //         var d = Vector2.Distance(snapToNeighborPos, newDragPosInCanvas);
        //         if (!(d < bestDistanceInCanvas))
        //             continue;
        //
        //         targetSnapPositionInCanvas = snapToNeighborPos;
        //         bestDistanceInCanvas = d;
        //     }
        // }
        //
        // var snapDistanceInCanvas = GraphCanvas.Current.InverseTransformDirection(new Vector2(20, 0)).X;
        // var isSnapping = bestDistanceInCanvas < snapDistanceInCanvas;

        // var moveDeltaOnCanvas = isSnapping
        //                             ? targetSnapPositionInCanvas - draggedNode.PosOnCanvas
        //                             : newDragPosInCanvas - draggedNode.PosOnCanvas;

        var moveDeltaOnCanvas = newDragPosInCanvas - draggedNode.PosOnCanvas;
        
        // Drag selection
        foreach (var e in _draggedNodes)
        {
            e.PosOnCanvas += moveDeltaOnCanvas;
        }
    }
    
    private void DragStart(HashSet<SnapGraphItem> draggedItems)
    {
        UpdateDragConnectionOnStart(draggedItems);
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

    private void DuringDrag(ICanvas canvas)
    {
        List<SnapGraphItem> draggedItems = new();

        var dragExtend = SnapGraphItem.GetGroupBoundingBox(draggedItems);
        dragExtend.Expand(SnapThreshold * canvas.Scale.X);

        var overlappingItems = new List<SnapGraphItem>();
        foreach (var otherItem in _layout.Items.Values)
        {
            if (otherItem.IsDragged || !dragExtend.Overlaps(otherItem.Area))
                continue;

            overlappingItems.Add(otherItem);
        }

        var bestSnapDistance = float.PositiveInfinity;
        Vector2 bestSnapDelta = default;

        // New possible ConnectionsOptions
        List<Symbol.Connection> newPossibleConnections = new();

        // Yes, that code looks weird.
        foreach (var otherItem in overlappingItems)
        {
            foreach (var draggedItem in draggedItems)
            {
                foreach (var draggedInAnchor in draggedItem.InputAnchors)
                {
                    foreach (var otherOutAnchor in otherItem.OutputAnchors)
                    {
                        var d = otherOutAnchor.GetSnapDistance(draggedInAnchor);
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

                foreach (var draggedOutAnchor in draggedItem.OutputAnchors)
                {
                    foreach (var otherInAnchor in otherItem.InputAnchors)
                    {
                        var d = otherInAnchor.GetSnapDistance(draggedOutAnchor);
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
        if (bestSnapDistance < SnapThreshold * canvas.Scale.X)
        {
            Log.Debug("Snapped by " + bestSnapDelta);
            foreach (var c in newPossibleConnections)
            {
                Log.Debug("new possible connection:" + c);
            }
        }
    }

    private const float SnapThreshold = 10;
    private readonly List<SnapGraphConnection> _bridgeConnectionsOnStart = new();
    
    private static bool _isDragging;
    private static Vector2 _dragStartPosInOpOnCanvas;

    private static ModifyCanvasElementsCommand _modifyCommand;

    private static Guid _draggedNodeId = Guid.Empty;
    private static List<ISelectableCanvasObject> _draggedNodes = new();
    private readonly SnapGraphCanvas _canvas;
    private readonly SnapGraphLayout _layout;
}