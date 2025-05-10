using ImGuiNET;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using T3.Editor.UiModel.Commands;
using T3.Editor.UiModel.Commands.Graph;
using T3.Editor.UiModel.Selection;

namespace T3.Editor.Gui.MagGraph.Interaction;

/// <summary>
/// Handles the dragging of annotations
/// </summary>
internal static class AnnotationDragging
{
    internal static void Draw(GraphUiContext context)
    {
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
                _dragStartDelta = ImGui.GetMousePos() - context.Canvas.TransformPosition(magAnnotation.PosOnCanvas);

                _draggedNodes.Clear();
                if (context.Selector.IsNodeSelected(annotation))
                {
                    _draggedNodes.AddRange(context.Selector.GetSelectedNodes<ISelectableCanvasObject>());
                }
                else
                {
                    if (!ImGui.GetIO().KeyCtrl)

                        _draggedNodes.AddRange(FindAnnotatedOps(context.ProjectView.InstView.SymbolUi, annotation));
                    _draggedNodes.Add(annotation);
                }

                _moveCommand = new ModifyCanvasElementsCommand(instViewSymbolUi, _draggedNodes, context.Selector);
            }

            if (started)
                return;
        }

        // Update dragging...
        {
            var newDragPos = ImGui.GetMousePos() - _dragStartDelta;
            var newDragPosInCanvas = context.Canvas.InverseTransformPositionFloat(newDragPos);
            var moveDeltaOnCanvas = newDragPosInCanvas - magAnnotation.PosOnCanvas;

            foreach (var e in _draggedNodes)
            {
                e.PosOnCanvas += moveDeltaOnCanvas;
            }
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
                if (!context.Selector.IsNodeSelected(annotation))
                {
                    if (!ImGui.GetIO().KeyShift)
                    {
                        context.Selector.Clear();
                    }

                    context.Selector.AddSelection(annotation);
                }
                else
                {
                    if (ImGui.GetIO().KeyShift)
                    {
                        context.Selector.DeselectNode(annotation, null);
                    }
                }
            }

            Log.Debug("Released -> Completed dragging");
            context.StateMachine.SetState(GraphStates.Default, context);
            _draggedNodes.Clear();
            _draggedAnnotationId = Guid.Empty;
            _moveCommand = null;
        }
    }

    private static List<ISelectableCanvasObject> FindAnnotatedOps(SymbolUi parentUi, Annotation annotation)
    {
        var matches = new List<ISelectableCanvasObject>();
        var aRect = new ImRect(annotation.PosOnCanvas, annotation.PosOnCanvas + annotation.Size);

        foreach (var n in parentUi.ChildUis.Values)
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

    private static Guid _draggedAnnotationId = Guid.Empty;
    private static Vector2 _dragStartDelta;
    private static ModifyCanvasElementsCommand _moveCommand;
    private static readonly List<ISelectableCanvasObject> _draggedNodes = [];
}