using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
// using T3.Editor.Gui.Graph;
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

public static class SnapItemMovement
{
    /// <summary>
    /// Reset to avoid accidental dragging of previous elements 
    /// </summary>
    public static void Reset()
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
        if (ImGui.IsMouseReleased(0) && _moveCommand != null)
        {
            Reset();    
        }
    }

    /// <summary>
    /// NOTE: This has to be called for ALL movable elements (ops, inputs, outputs) and directly after ImGui.Item
    /// </summary>
    public static void Handle(SnapGraphItem item, ScalableCanvas canvas)
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

            _moveCommand = new ModifyCanvasElementsCommand(compositionSymbolId, _draggedNodes);
            //ShakeDetector.ResetShaking();
        }
        else if (isActiveNode && ImGui.IsMouseDown(ImGuiMouseButton.Left) && _moveCommand != null)
        {
            // TODO: Implement shake disconnect later
            HandleNodeDragging(item, canvas);
        }
        else if (isActiveNode && ImGui.IsMouseReleased(0) && _moveCommand != null)
        {
            if (_draggedNodeId != item.Id)
                return;
                
            var singleDraggedNode = (_draggedNodes.Count == 1) ? _draggedNodes[0] : null;
            _draggedNodeId = Guid.Empty;
            _draggedNodes.Clear();

            var wasDragging = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).LengthSquared() > UserSettings.Config.ClickThreshold;
            if (wasDragging)
            {
                _moveCommand.StoreCurrentValues();

                if (singleDraggedNode != null && ConnectionSplitHelper.BestMatchLastFrame != null && singleDraggedNode is SymbolChildUi childUi)
                {
                    var instanceForSymbolChildUi = composition.Children.SingleOrDefault(child => child.SymbolChildId == childUi.Id);
                    ConnectionMaker.SplitConnectionWithDraggedNode(childUi, 
                                                                   ConnectionSplitHelper.BestMatchLastFrame.Connection, 
                                                                   instanceForSymbolChildUi,
                                                                   _moveCommand);
                    _moveCommand = null;
                }
                else
                {
                    UndoRedoStack.Add(_moveCommand);
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
            && !NodeSelection.IsNodeSelected(item))
        {
            NodeSelection.SetSelectionToChildUi(item.SymbolChildUi, item.Instance);
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
Log.Debug(" delta:" + moveDeltaOnCanvas);
        
        // Drag selection
        foreach (var e in _draggedNodes)
        {
            e.PosOnCanvas += moveDeltaOnCanvas;
        }
    }
    

    private static bool _isDragging;
    private static Vector2 _dragStartPosInOpOnCanvas;

    private static ModifyCanvasElementsCommand _moveCommand;

    private static Guid _draggedNodeId = Guid.Empty;
    private static List<ISelectableCanvasObject> _draggedNodes = new();
}