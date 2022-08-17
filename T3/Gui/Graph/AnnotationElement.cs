using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using System.Numerics;
using T3.Core.IO;
using T3.Core.Operator;
using T3.Gui.Commands;
using t3.Gui.Commands.Graph;
using T3.Gui.Graph;
using T3.Gui.Graph.Interaction;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.TypeColors;
using T3.Gui.UiHelpers;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Draws an AnnotationElement and handles its interaction
    /// </summary>
    static class AnnotationElement
    {

        public static void StartRenaming(Annotation annotation)
        {
            _requestedRenameId = annotation.Id;
        }
        
        private static Guid _requestedRenameId = Guid.Empty;
        
        internal static void Draw(Annotation annotation)
        {
            ImGui.PushID(annotation.Id.GetHashCode());
            {
                _screenArea = GraphCanvas.Current.TransformRect(new ImRect(annotation.PosOnCanvas, annotation.PosOnCanvas + annotation.Size));
                var titleSize = annotation.Size;
                titleSize.Y = MathF.Min(titleSize.Y, 14);

                // Keep height of title area at a minimum height when zooming out
                var clickableArea = GraphCanvas.Current.TransformRect(new ImRect(annotation.PosOnCanvas, annotation.PosOnCanvas + titleSize));
                var height = MathF.Min(16, _screenArea.GetHeight());
                clickableArea.Max.Y = clickableArea.Min.Y + height;

                _isVisible = ImGui.IsRectVisible(_screenArea.Min, _screenArea.Max);

                if (!_isVisible)
                    return;

                var drawList = GraphCanvas.Current.DrawList;

                // Resize indicator
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNWSE);
                    ImGui.SetCursorScreenPos(_screenArea.Max - new Vector2(10, 10));
                    ImGui.Button("##resize", new Vector2(10, 10));
                    if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                    {
                        var delta = GraphCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta);
                        annotation.Size += delta;
                    }

                    ImGui.SetMouseCursor(ImGuiMouseCursor.Arrow);
                }

                // Background
                const float backgroundAlpha = 0.1f;
                const float headerHoverAlpha = 0.2f;
                
                drawList.AddRectFilled(_screenArea.Min, _screenArea.Max, annotation.Color.Fade(backgroundAlpha));

                // Interaction
                ImGui.SetCursorScreenPos(clickableArea.Min);
                ImGui.InvisibleButton("##annotationHeader", clickableArea.GetSize());

                THelpers.DebugItemRect();
                var isHeaderHovered = ImGui.IsItemHovered();
                if (isHeaderHovered)
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }
                
                // Header
                drawList.AddRectFilled(clickableArea.Min, clickableArea.Max,
                                       annotation.Color.Fade(isHeaderHovered ? headerHoverAlpha : 0));

                HandleDragging(annotation);
                var shouldRename = (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) || _requestedRenameId == annotation.Id;
                Renaming.Draw(annotation, shouldRename);
                if (shouldRename)
                {
                    _requestedRenameId = Guid.Empty;
                }

                var borderColor = annotation.IsSelected
                                      ? Color.White
                                      : annotation.Color.Fade(isHeaderHovered ? headerHoverAlpha : backgroundAlpha);
                
                const float thickness = 1;
                drawList.AddRect(_screenArea.Min - Vector2.One * thickness,
                                 _screenArea.Max + Vector2.One * thickness,
                                 borderColor, 
                                 0f, 
                                 0, 
                                 thickness);

                // Label
                {
                    var isScaledDown = GraphCanvas.Current.Scale.X < 1;
                    ImGui.PushFont(isScaledDown ? Fonts.FontSmall : Fonts.FontBold);

                    drawList.PushClipRect(_screenArea.Min, _screenArea.Max, true);
                    var labelPos = _screenArea.Min + new Vector2(4, 4);

                    drawList.AddText(labelPos,
                                     ColorVariations.OperatorLabel.Apply(annotation.Color),
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
                        _draggedNodes = NodeSelection.GetSelectedNodes<ISelectableCanvasObject>().ToList();
                    }
                    else
                    {
                        var parentUi = SymbolUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];

                        if (!ImGui.GetIO().KeyCtrl)
                            _draggedNodes = FindAnnotatedOps(parentUi, annotation).ToList();
                        _draggedNodes.Add(annotation);
                    }

                    _moveCommand = new ModifyCanvasElementsCommand(compositionSymbolId, _draggedNodes);
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

                // var singleDraggedNode = (_draggedNodes.Count == 1) ? _draggedNodes[0] : null;
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
                    if (!NodeSelection.IsNodeSelected(annotation))
                    {
                        if (!ImGui.GetIO().KeyShift)
                        {
                            NodeSelection.Clear();
                        }

                        NodeSelection.AddSelection(annotation);
                    }
                    else
                    {
                        if (ImGui.GetIO().KeyShift)
                        {
                            NodeSelection.DeselectNode(annotation, instance);
                        }
                    }
                }

                _moveCommand = null;
            }

            var wasDraggingRight = ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Length() > UserSettings.Config.ClickThreshold;
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right)
                && !wasDraggingRight
                && ImGui.IsItemHovered()
                && !NodeSelection.IsNodeSelected(annotation))
            {
                NodeSelection.SetSelection(annotation);
            }
        }

        private static List<ISelectableCanvasObject> FindAnnotatedOps(SymbolUi parentUi, Annotation annotation)
        {
            var matches = new List<ISelectableCanvasObject>();
            var aRect = new ImRect(annotation.PosOnCanvas, annotation.PosOnCanvas + annotation.Size);

            foreach (var n in parentUi.ChildUis)
            {
                var nRect = new ImRect(n.PosOnCanvas, n.PosOnCanvas + n.Size);
                if (aRect.Contains(nRect))
                    matches.Add(n);
            }

            foreach (var a in parentUi.Annotations.Values)
            {
                if (a == annotation)
                    continue;
                
                var nRect = new ImRect(a.PosOnCanvas, a.PosOnCanvas + a.Size);
                if (aRect.Contains(nRect))
                    matches.Add(a);
            }

            return matches;
        }

        private static void HandleNodeDragging(ISelectableCanvasObject draggedNode)
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

        private static class Renaming
        {
            public static void Draw(Annotation annotation, bool shouldBeOpened)
            {
                var justOpened = false;
                if (_focusedAnnotationId == Guid.Empty)
                {
                    if (shouldBeOpened)
                    {
                        justOpened = true;
                        ImGui.SetKeyboardFocusHere();
                        _focusedAnnotationId = annotation.Id;
                        _changeAnnotationTextCommand = new ChangeAnnotationTextCommand(annotation, annotation.Title);
                    }
                }

                if (_focusedAnnotationId == Guid.Empty)
                    return;

                if (_focusedAnnotationId != annotation.Id)
                    return;

                var positionInScreen = _screenArea.Min;
                ImGui.SetCursorScreenPos(positionInScreen);

                var text = annotation.Title;

                ImGui.SetNextItemWidth(150);
                ImGui.InputTextMultiline("##renameAnnotation", ref text, 256, _screenArea.GetSize(), ImGuiInputTextFlags.AutoSelectAll);
                if (!ImGui.IsItemDeactivated())
                    annotation.Title = text;

                if (!justOpened && (ImGui.IsItemDeactivated() || ImGui.IsKeyPressed((ImGuiKey)Key.Esc)))
                {
                    _focusedAnnotationId = Guid.Empty;
                    _changeAnnotationTextCommand.NewText = annotation.Title;
                    UndoRedoStack.AddAndExecute(_changeAnnotationTextCommand);
                }
            }

            private static Guid _focusedAnnotationId;
            private static ChangeAnnotationTextCommand _changeAnnotationTextCommand;
        }

        private static bool _isDragging;
        private static Vector2 _dragStartDelta;
        private static ModifyCanvasElementsCommand _moveCommand;

        private static Guid _draggedNodeId = Guid.Empty;
        private static List<ISelectableCanvasObject> _draggedNodes = new();

        private static bool _isVisible;
        private static ImRect _screenArea;
    }
}