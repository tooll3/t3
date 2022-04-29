using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Gui.Commands;
using T3.Gui.Interaction.Variations.Model;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;

namespace T3.Gui.Windows.Variations
{
    public static class VariationThumbnail
    {
        public static void Draw(VariationCanvas canvas, Variation v, ImDrawListPtr drawList)
        {
            _canvas = canvas;
            var pMin = canvas.TransformPosition(v.PosOnCanvas);
            var sizeOnScreen = canvas.TransformDirectionFloored(ThumbnailSize);
            var pMax = pMin + sizeOnScreen;

            ImGui.PushClipRect(pMin, pMax, true);

            drawList.AddRectFilled(pMin, pMax, Color.Black);
            drawList.AddRect(pMin, pMax, Color.Gray);
            ImGui.PushFont(Fonts.FontSmall);

            drawList.AddRectFilledMultiColor(pMin + new Vector2(0, sizeOnScreen.Y - 20),
                                             pMax,
                                             Color.TransparentBlack,
                                             Color.TransparentBlack,
                                             Color.Black,
                                             Color.Black
                                            );

            drawList.AddText(pMin + new Vector2(4, sizeOnScreen.Y - 20),
                             Color.White,
                             string.IsNullOrEmpty(v.Title) ? "Untitled" : v.Title);

            drawList.AddText(pMin + new Vector2(sizeOnScreen.X - 20, sizeOnScreen.Y - 20),
                             Color.White,
                             $"{v.ActivationIndex:00}");

            ImGui.PopFont();
            ImGui.SetCursorScreenPos(pMin);
            ImGui.InvisibleButton("##thumbnail", ThumbnailSize);
            HandleMovement(v);

            ImGui.PopClipRect();
        }

        private static VariationCanvas _canvas;
        private static CanvasElementSelection _selection => _canvas._selection;
        

        /// <summary>
        /// NOTE: This has to be called directly after ImGui.Item
        /// </summary>
        public static void HandleMovement(ISelectableCanvasObject node)
        {
            if (ImGui.IsItemActive())
            {
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    _draggedNodeId = node.Id;
                    if (node.IsSelected)
                    {
                        _draggedNodes = _selection.GetSelectedNodes<ISelectableCanvasObject>().ToList();
                    }
                    else
                    {
                        _draggedNodes.Add(node);
                    }

                    _moveCommand = new ModifyCanvasElementsCommand(_canvas, _draggedNodes);
                }

                HandleNodeDragging(node);
            }
            else if (ImGui.IsMouseReleased(0) && _moveCommand != null)
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
                    UndoRedoStack.Add(_moveCommand);
                }
                else
                {
                    if (!_selection.IsNodeSelected(node))
                    {
                        if (!ImGui.GetIO().KeyShift)
                        {
                            _selection.Clear();
                        }

                        _selection.AddSelection(node);
                    }
                    else
                    {
                        if (ImGui.GetIO().KeyShift)
                        {
                            _selection.DeselectNode(node);
                        }
                    }
                }

                _moveCommand = null;
            }
            
            // Select for context menu with right mouse click
            // var wasDraggingRight = ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Length() > UserSettings.Config.ClickThreshold;
            // if (ImGui.IsMouseReleased(ImGuiMouseButton.Right)
            //     && !wasDraggingRight
            //     && ImGui.IsItemHovered()
            //     && !VariationThumbnailSelection.IsNodeSelected(node))
            // {
            //     if (node is SymbolChildUi childUi2)
            //     {
            //         VariationThumbnailSelection.SetSelectionToChildUi(childUi2, instance);
            //     }
            //     else
            //     {
            //         VariationThumbnailSelection.SetSelection(node);
            //     }
            // }
        }

        private static bool _isDragging;
        
        private static void HandleNodeDragging(ISelectableCanvasObject draggedNode)
        {
            if (!ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                _isDragging = false;
                return;
            }

            if (!_isDragging)
            {
                _dragStartDelta = ImGui.GetMousePos() - _canvas.TransformPosition(draggedNode.PosOnCanvas);
                _isDragging = true;
            }

            var newDragPos = ImGui.GetMousePos() - _dragStartDelta;
            var newDragPosInCanvas = _canvas.InverseTransformPosition(newDragPos);

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

                foreach (var neighbor in _canvas.GetSelectables())
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

            var snapDistanceInCanvas = _canvas.InverseTransformDirection(new Vector2(20, 0)).X;
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

        private static readonly Vector2 SnapPadding = new Vector2(40, 20);
        private static readonly Vector2[] _snapOffsetsInCanvas =
            {
                new Vector2(SymbolChildUi.DefaultOpSize.X + SnapPadding.X, 0),
                new Vector2(-SymbolChildUi.DefaultOpSize.X - +SnapPadding.X, 0),
                new Vector2(0, SnapPadding.Y),
                new Vector2(0, -SnapPadding.Y)
            };
        
        
        private static Guid _draggedNodeId;
        private static List<ISelectableCanvasObject> _draggedNodes;
        public static readonly Vector2 ThumbnailSize = new Vector2(160, 160 / 16f * 9);
        private static ModifyCanvasElementsCommand _moveCommand;
        private static Vector2 _dragStartDelta;
    }
}