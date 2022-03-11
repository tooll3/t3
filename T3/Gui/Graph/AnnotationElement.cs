using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Graph.Interaction;
using T3.Gui.InputUi;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.TypeColors;
using T3.Gui.UiHelpers;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Draws published input parameters of a <see cref="Symbol"/> and uses <see cref="ConnectionMaker"/> 
    /// create new connections with it.
    /// </summary>
    static class AnnotationElement
    {
        internal static void Draw(Annotation annotation)
        {
            ImGui.PushID(annotation.Id.GetHashCode());
            {
                var lastScreenRect = GraphCanvas.Current.TransformRect(new ImRect(annotation.PosOnCanvas, annotation.PosOnCanvas + annotation.Size));
                var titleSize = annotation.Size;
                titleSize.Y = MathF.Min(titleSize.Y, 20);

                var lastClickableRect = GraphCanvas.Current.TransformRect(new ImRect(annotation.PosOnCanvas, annotation.PosOnCanvas + titleSize));

                _isVisible = ImGui.IsRectVisible(lastScreenRect.Min, lastScreenRect.Max);

                if (!_isVisible)
                    return;

                // Resize indicator
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNWSE);
                    ImGui.SetCursorScreenPos(lastScreenRect.Max - new Vector2(10, 10));
                    ImGui.Button("##resize", new Vector2(10, 10));
                    if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                    {
                        var delta = GraphCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta);
                        annotation.Size += delta;
                    }

                    ImGui.SetMouseCursor(ImGuiMouseCursor.Arrow);
                }

                // Interaction
                ImGui.SetCursorScreenPos(lastClickableRect.Min);
                ImGui.InvisibleButton("node", lastClickableRect.GetSize());

                THelpers.DebugItemRect();
                var hovered = ImGui.IsItemHovered();
                if (hovered)
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                HandleDragging(annotation);

                // Rendering
                var typeColor = Color.Gray;

                var drawList = GraphCanvas.Current.DrawList;
                drawList.AddRectFilled(lastScreenRect.Min, lastScreenRect.Max,
                                       hovered
                                           ? ColorVariations.OperatorHover.Apply(typeColor)
                                           : ColorVariations.ConnectionLines.Apply(typeColor));

                if (annotation.IsSelected)
                {
                    const float thickness = 1;
                    drawList.AddRect(lastScreenRect.Min - Vector2.One * thickness,
                                     lastScreenRect.Max + Vector2.One * thickness,
                                     Color.White, 0f, 0, thickness);
                }

                // Label
                {
                    var isScaledDown = GraphCanvas.Current.Scale.X < 1;
                    ImGui.PushFont(isScaledDown ? Fonts.FontSmall : Fonts.FontBold);

                    drawList.PushClipRect(lastScreenRect.Min, lastScreenRect.Max, true);
                    var labelPos = lastScreenRect.Min + new Vector2(4, 4);

                    drawList.AddText(labelPos,
                                     ColorVariations.OperatorLabel.Apply(typeColor),
                                     annotation.Title);
                    ImGui.PopFont();
                    drawList.PopClipRect();
                }
            }
            ImGui.PopID();
        }

        private static void HandleDragging(Annotation annotation, Instance instance = null)
        {
            if (ImGui.IsItemActive())
            {
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    var compositionSymbolId = GraphCanvas.Current.CompositionOp.Symbol.Id;
                    _draggedNodeId = annotation.Id;
                    if (annotation.IsSelected)
                    {
                        _draggedNodes = SelectionManager.GetSelectedNodes<ISelectableNode>().ToList();
                    }
                    else
                    {
                        var parentUi = SymbolUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];

                        //if (UserSettings.Config.SmartGroupDragging)
                        _draggedNodes = FindAnnotatedOps(parentUi, annotation).ToList();

                        _draggedNodes.Add(annotation);
                    }

                    _moveCommand = new ChangeSelectableCommand(compositionSymbolId, _draggedNodes);
                }
                else if (_moveCommand != null)
                {
                }

                HandleNodeDragging(annotation);
            }
            else if (ImGui.IsMouseReleased(0) && _moveCommand != null)
            {
                if (_draggedNodeId != annotation.Id)
                    return;

                var singleDraggedNode = (_draggedNodes.Count == 1) ? _draggedNodes[0] : null;
                _draggedNodeId = Guid.Empty;
                _draggedNodes.Clear();

                var wasDragging = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).LengthSquared() > UserSettings.Config.ClickTreshold;
                if (wasDragging)
                {
                    _moveCommand.StoreCurrentValues();
                    UndoRedoStack.Add(_moveCommand);

                    if (singleDraggedNode != null && ConnectionMaker.ConnectionSplitHelper.BestMatchLastFrame != null &&
                        singleDraggedNode is SymbolChildUi childUi)
                    {
                        var instanceForSymbolChildUi =
                            GraphCanvas.Current.CompositionOp.Children.SingleOrDefault(child => child.SymbolChildId == childUi.Id);
                        ConnectionMaker.SplitConnectionWithDraggedNode(childUi,
                                                                       ConnectionMaker.ConnectionSplitHelper.BestMatchLastFrame.Connection,
                                                                       instanceForSymbolChildUi);
                    }

                    // Reorder inputs nodes if dragged
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
                    if (!SelectionManager.IsNodeSelected(annotation))
                    {
                        if (!ImGui.GetIO().KeyShift)
                        {
                            SelectionManager.Clear();
                        }

                        SelectionManager.AddSelection(annotation);
                    }
                    else
                    {
                        if (ImGui.GetIO().KeyShift)
                        {
                            SelectionManager.DeselectNode(annotation, instance);
                        }
                    }
                }

                _moveCommand = null;
            }

            var wasDraggingRight = ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Length() > UserSettings.Config.ClickTreshold;
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right)
                && !wasDraggingRight
                && ImGui.IsItemHovered()
                && !SelectionManager.IsNodeSelected(annotation))
            {
                SelectionManager.SetSelection(annotation);
            }
        }

        private static List<ISelectableNode> FindAnnotatedOps(SymbolUi parentUi, Annotation annotation)
        {
            var matches = new List<ISelectableNode>();
            var aRect = new ImRect(annotation.PosOnCanvas, annotation.PosOnCanvas + annotation.Size);

            foreach (var n in parentUi.ChildUis)
            {
                var nRect = new ImRect(n.PosOnCanvas, n.PosOnCanvas + n.Size);
                if (aRect.Contains(nRect))
                    matches.Add(n);
            }

            return matches;
        }

        private static void HandleNodeDragging(ISelectableNode draggedNode)
        {
            if (!ImGui.IsMouseDragging(ImGuiMouseButton.Left))
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
            var moveDeltaOnCanvas = newDragPosInCanvas - draggedNode.PosOnCanvas;

            // Drag selection
            foreach (var e in _draggedNodes)
            {
                e.PosOnCanvas += moveDeltaOnCanvas;
            }
        }

        private static bool _isDragging;
        private static Vector2 _dragStartDelta;
        private static ChangeSelectableCommand _moveCommand;

        private static Guid _draggedNodeId = Guid.Empty;
        private static List<ISelectableNode> _draggedNodes = new List<ISelectableNode>();

        private static bool _isVisible;
    }
}