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

            drawList.AddRectFilled(pMin, pMax, Color.DarkGray);
            drawList.AddRect(pMin, pMax, Color.Gray.Fade(0.2f));

            v.IsSelected = _selection.IsNodeSelected(v);
            if (v.IsSelected)
            {
                drawList.AddRect(pMin - Vector2.One, pMax + Vector2.One, Color.White);
            }

            var bottomPadding = 15;
            drawList.AddRectFilledMultiColor(pMin + new Vector2(1, sizeOnScreen.Y - bottomPadding),
                                             pMax - Vector2.One,
                                             Color.TransparentBlack,
                                             Color.TransparentBlack,
                                             Color.Black.Fade(0.4f),
                                             Color.Black.Fade(0.4f)
                                            );
            ImGui.PushClipRect(pMin, pMax, true);
            ImGui.PushFont(Fonts.FontSmall);

            drawList.AddText(pMin + new Vector2(4, sizeOnScreen.Y - bottomPadding),
                             Color.White.Fade(0.6f),
                             string.IsNullOrEmpty(v.Title) ? "Untitled" : v.Title);

            drawList.AddText(pMin + new Vector2(sizeOnScreen.X - bottomPadding, sizeOnScreen.Y - bottomPadding),
                             Color.White.Fade(0.3f),
                             $"{v.ActivationIndex:00}");


            ImGui.PopFont();
            ImGui.SetCursorScreenPos(pMin);
            ImGui.PushID(v.Id.GetHashCode());
            ImGui.InvisibleButton("##thumbnail", ThumbnailSize);
            HandleMovement(v);
            ImGui.PopID();

            ImGui.PopClipRect();
        }

        private static VariationCanvas _canvas;
        private static CanvasElementSelection _selection => _canvas._selection;

        private static void HandleMovement(ISelectableCanvasObject node)
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
                foreach (var neighbor in _canvas.GetSelectables())
                {
                    if (neighbor == draggedNode || _draggedNodes.Contains(neighbor))
                        continue;
            
                    var snapToNeighborPos = neighbor.PosOnCanvas + offset;
            
                    var d = Vector2.Distance(snapToNeighborPos, newDragPosInCanvas);
                    if (!(d < bestDistanceInCanvas))
                        continue;
            
                    targetSnapPositionInCanvas = snapToNeighborPos;
                    bestDistanceInCanvas = d;
                }
            }
            
            var snapDistanceInCanvas = _canvas.InverseTransformDirection(new Vector2(6, 6)).Length();
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

        private static Guid _draggedNodeId;
        private static List<ISelectableCanvasObject> _draggedNodes = new();
        public static readonly Vector2 ThumbnailSize = new Vector2(160, (int)(160 / 16f * 9));
        
        private static readonly Vector2 _snapPadding = new Vector2(3, 3);
        private static readonly Vector2[] _snapOffsetsInCanvas =
            {
                new(ThumbnailSize.X + _snapPadding.X, 0),
                new(-ThumbnailSize.X - _snapPadding.X, 0),
                new(0, ThumbnailSize.Y + _snapPadding.Y),
                new(0, -ThumbnailSize.Y - _snapPadding.Y)
            };        
        private static ModifyCanvasElementsCommand _moveCommand;
        private static Vector2 _dragStartDelta;
    }
}