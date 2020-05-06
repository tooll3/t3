using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.InputUi;
using T3.Gui.Selection;
using UiHelpers;

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
                }

                HandleNodeDragging(node);
            }
            else if (ImGui.IsMouseReleased(0) && _moveCommand != null)
            {
                if (_draggedNodeId != node.Id)
                    return;

                _draggedNodeId = Guid.Empty;
                _draggedNodes.Clear();

                var varDragging = ImGui.GetMouseDragDelta(0).LengthSquared() > 0.0f;
                if (varDragging)
                {
                    Log.Debug("was dragging node " + node);
                    _moveCommand.StoreCurrentValues();
                    UndoRedoStack.Add(_moveCommand);

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
        }

        private static Guid _draggedNodeId = Guid.Empty;
        private static List<ISelectableNode> _draggedNodes = new List<ISelectableNode>();

        private static void HandleNodeDragging(ISelectableNode draggedNode)
        {
            if (!ImGui.IsMouseDragging(0))
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

        

        private static double RecursivelyAlignChildrenOfSelectedOps(SymbolChildUi childUi, List<ISelectableNode> freshlySnappedOpWidgets)
        {
            var connectedInstances = GetConnectedInstances(childUi);
            var parentUi = SymbolUiRegistry.Entries[GraphCanvas.Current.CompositionOp.SymbolChildId];
            var connectedChildUis = connectedInstances
                                   .Select(ci => parentUi.ChildUis.Single(childUi2 => childUi2.Id == ci.SymbolChildId))
                                   .ToList();
            
            var compositionSymbol = GraphCanvas.Current.CompositionOp.Symbol;
            
            // var childUis = (from con in compositionSymbol.Connections
            //                 where !con.IsConnectedToSymbolInput && !con.IsConnectedToSymbolOutput
            //                 from childUi in compositionSymbolUi.ChildUis
            //                 where con.SourceParentOrChildId == childUi.Id
            //                 select childUi).Distinct();            

            foreach (var connectedChild in connectedChildUis)
            {
                if (childUi.IsSelected)
                    continue;
                
                
            }
            return 0;
        }

        private static List<Instance> GetConnectedInstances(SymbolChildUi childUi)
        {
            var instance = GraphCanvas.Current.CompositionOp.Children.Single(child => child.SymbolChildId == childUi.Id);

            var connectedInstances = new List<Instance>();
            foreach (var i in instance.Inputs)
            {
                if (!i.IsConnected)
                    continue;

                var connection = instance.Parent.Symbol.Connections.Single(c => c.TargetParentOrChildId == instance.SymbolChildId);
                connectedInstances.Add(instance.Parent.Children.Single(child => child.SymbolChildId == connection.SourceParentOrChildId));
            }

            return connectedInstances;
        }

        private enum Alignment
        {
            NoSnapped,
            SnappedBelow,
            SnappedAbove,
            SnappedToRight,
            SnappedToLeft,
        }

        public static readonly Vector2 SnapPadding = new Vector2(20, 20);

        public static readonly Vector2[] SnapOffsetsInCanvas =
            {
                new Vector2(SymbolChildUi.DefaultOpSize.X + SnapPadding.X, 0),
                new Vector2(-SymbolChildUi.DefaultOpSize.X - +SnapPadding.X, 0),
                new Vector2(0, SnapPadding.Y),
                new Vector2(0, -SnapPadding.Y)
            };

        private static bool _isDragging;
        private static Vector2 _dragStartDelta;
        private static ChangeSelectableCommand _moveCommand;
    }
}