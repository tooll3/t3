using ImGuiNET;
using System;
using System.Diagnostics;
using System.Numerics;
using T3.Core.Logging;
using T3.Gui.Commands;
using T3.Gui.Selection;

namespace T3.Gui.Graph
{
    public static class SelectableMovement
    {
        private static ChangeSelectableCommand _moveCommand = null;

        public static void Handle(ISelectable selectable)
        {
            if (ImGui.IsItemActive())
            {
                if (ImGui.IsItemClicked(0))
                {
                    var handler = GraphCanvas.Current.SelectionHandler;

                    if (!handler.SelectedElements.Contains(selectable))
                    {
                        if (!ImGui.GetIO().KeyShift)
                        {
                            handler.SetElement(selectable);
                        }
                        else
                        {
                            handler.AddElement(selectable);
                        }
                    }
                    else
                    {
                        if (ImGui.GetIO().KeyShift)
                        {
                            handler.RemoveElement(selectable);
                        }
                    }

                    Guid compositionSymbolId = GraphCanvas.Current.CompositionOp.Symbol.Id;
                    _moveCommand = new ChangeSelectableCommand(compositionSymbolId, handler.SelectedElements);
                }

                if (ImGui.IsMouseDragging(0))
                {
                    if (!_isDragging)
                        _dragStartDelta = ImGui.GetMousePos() - GraphCanvas.Current.TransformPosition(selectable.PosOnCanvas);

                    _isDragging = true;

                    var newDragPos = ImGui.GetMousePos() - _dragStartDelta;
                    var newDragPosInCanvas = GraphCanvas.Current.InverseTransformPosition(newDragPos);
                    //var snapDelta = new Vector2(140, 0);

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
                            if (neighbor.IsSelected || neighbor == selectable)
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
                                                ? targetSnapPositionInCanvas - selectable.PosOnCanvas
                                                : newDragPosInCanvas - selectable.PosOnCanvas;

                    // Drag selection
                    foreach (var e in GraphCanvas.Current.SelectionHandler.SelectedElements)
                    {
                        e.PosOnCanvas += moveDeltaOnCanvas;
                    }
                }
                else
                {
                    _isDragging = false;
                }
            }
            else if (ImGui.IsMouseReleased(0) && _moveCommand != null)
            {
                if (ImGui.GetMouseDragDelta(0).LengthSquared() > 0.0f)
                {
                    // add to stack
                    Log.Debug("Added to undo stack");
                    _moveCommand.StoreCurrentValues();
                    UndoRedoStack.Add(_moveCommand);
                }

                _moveCommand = null;
            }
        }

        private static readonly Vector2 SnapPadding = new Vector2(20,20);

        private static Vector2[] SnapOffsetsInCanvas = new Vector2[]
                                               {
                                                   new Vector2(SymbolChildUi.DefaultOpSize.X + SnapPadding.X, 0),
                                                   new Vector2(-SymbolChildUi.DefaultOpSize.X-  + SnapPadding.X, 0),
                                                   new Vector2(0,  SnapPadding.Y),
                                                   new Vector2(0, -SnapPadding.Y),
                                               };

        private static bool _isDragging = false;
        private static Vector2 _dragStartDelta;
    }
}