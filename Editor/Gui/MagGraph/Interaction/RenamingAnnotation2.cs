#nullable enable
using ImGuiNET;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel.Commands;
using T3.Editor.UiModel.Commands.Annotations;
using T3.SystemUi;

namespace T3.Editor.Gui.MagGraph.Interaction;

/// <summary>
/// Handles renaming annotation titles
/// </summary>
internal static class RenamingAnnotation2
{
    public static void Draw(GraphUiContext context)
    {
        var annotationId = context.ActiveAnnotationId;

        if (!context.Layout.Annotations.TryGetValue(annotationId, out var magAnnotation))
        {
            context.ActiveAnnotationId = Guid.Empty;
            context.StateMachine.SetState(GraphStates.Default, context);
            return;
        }
        
        var annotation = magAnnotation.Annotation;
        var screenArea = context.Canvas.TransformRect(ImRect.RectWithSize(annotation.PosOnCanvas, annotation.Size));
        
        var justOpened = _focusedAnnotationId != annotationId;
        if (justOpened)
        {
            
            ImGui.SetKeyboardFocusHere();
            _focusedAnnotationId = annotationId;
            _changeAnnotationTextCommand = new ChangeAnnotationTextCommand(annotation, annotation.Title);
        }
        
        var positionInScreen = screenArea.Min;
        ImGui.SetCursorScreenPos(positionInScreen);

        var text = annotation.Title;

        ImGui.SetNextItemWidth(150);
        
        // Note: As of imgui 1.89 AutoSelectAll might not be supported for InputTextMultiline
        ImGui.InputTextMultiline("##renameAnnotation", ref text, 256, screenArea.GetSize(), ImGuiInputTextFlags.AutoSelectAll);
        if (!ImGui.IsItemDeactivated())
            annotation.Title = text;
        
        if (justOpened || _changeAnnotationTextCommand == null)
            return;
        
        var shouldClose =  !ImGui.IsItemDeactivated() && !ImGui.IsKeyPressed((ImGuiKey)Key.Esc);
        if (shouldClose) 
            return;
        
        _focusedAnnotationId = Guid.Empty;
        _changeAnnotationTextCommand.NewText = annotation.Title;
        context.ActiveAnnotationId = Guid.Empty;
            
        UndoRedoStack.AddAndExecute(_changeAnnotationTextCommand);
        context.StateMachine.SetState(GraphStates.Default, context);
    }

    private static Guid _focusedAnnotationId;
    private static ChangeAnnotationTextCommand? _changeAnnotationTextCommand;
}