using ImGuiNET;
using T3.Editor.Gui.Interaction.Snapping;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel.Commands;
using T3.Editor.UiModel.Commands.Graph;

namespace T3.Editor.Gui.MagGraph.Interaction;

internal static class AnnotationResizing
{
    internal static void Draw(GraphUiContext context)
    {
        _snapHandlerY.DrawSnapIndicator(context.Canvas, UiColors.ForegroundFull.Fade(0.1f));
        _snapHandlerX.DrawSnapIndicator(context.Canvas, UiColors.ForegroundFull.Fade(0.1f));

        // Setup...
        var instViewSymbolUi = context?.ProjectView?.InstView?.SymbolUi;
        if (instViewSymbolUi == null)
            return;

        var annotationId = context.ActiveAnnotationId;

        if (!context.Layout.Annotations.TryGetValue(annotationId, out var magAnnotation))
        {
            context.ActiveAnnotationId = Guid.Empty;
            context.StateMachine.SetState(GraphStates.Default, context);
            return;
        }

        var annotation = magAnnotation.Annotation;

        // Start dragging...
        {
            var started = context.ActiveAnnotationId != _draggedAnnotationId;
            if (started)
            {
                _draggedAnnotationId = context.ActiveAnnotationId;
                _dragStartDelta = ImGui.GetMousePos() - context.Canvas.TransformPosition(magAnnotation.PosOnCanvas + magAnnotation.Size);
                _moveCommand = new ModifyCanvasElementsCommand(instViewSymbolUi, [annotation], context.Selector);
            }

            if (started)
                return;
        }

        // Update dragging...
        {
            var newDragPos = ImGui.GetMousePos() - _dragStartDelta;
            var newDragPosInCanvas = context.Canvas.InverseTransformPositionFloat(newDragPos);

            if (_snapHandlerX.TryCheckForSnapping(newDragPosInCanvas.X, out var snappedPosX,
                                                  context.Canvas.Scale.X * 0.25f,
                                                      [magAnnotation],
                                                  context.Layout.Annotations.Values
                                                 ))
            {
                newDragPosInCanvas.X = (float)snappedPosX;
            }

            if (_snapHandlerY.TryCheckForSnapping(newDragPosInCanvas.Y, out var snappedPosY,
                                                  context.Canvas.Scale.Y * 0.25f,
                                                      [magAnnotation],
                                                  context.Layout.Annotations.Values
                                                 ))
            {
                newDragPosInCanvas.Y = (float)snappedPosY;
            }

            annotation.Size = newDragPosInCanvas - annotation.PosOnCanvas;
        }

        // Complete dragging...
        var completed = ImGui.IsMouseReleased(ImGuiMouseButton.Left);
        if (completed)
        {
            var wasDragging = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).LengthSquared() > UserSettings.Config.ClickThreshold;
            if (wasDragging)
            {
                _moveCommand.StoreCurrentValues();
                UndoRedoStack.Add(_moveCommand);
            }
            else
            {
                _moveCommand.Undo();
                if (context.Selector.IsNodeSelected(annotation))
                {
                    if (ImGui.GetIO().KeyShift)
                    {
                        context.Selector.DeselectNode(annotation, null);
                    }
                }
                else
                {
                    if (!ImGui.GetIO().KeyShift)
                        context.Selector.Clear();

                    context.Selector.AddSelection(annotation);
                }
            }

            context.StateMachine.SetState(GraphStates.Default, context);
            _draggedAnnotationId = Guid.Empty;
            _moveCommand = null;
        }
    }

    private static Guid _draggedAnnotationId = Guid.Empty;
    private static Vector2 _dragStartDelta;
    private static ModifyCanvasElementsCommand _moveCommand;

    private static readonly ValueSnapHandler _snapHandlerX = new(SnapResult.Orientations.Horizontal);
    private static readonly ValueSnapHandler _snapHandlerY = new(SnapResult.Orientations.Vertical);
}