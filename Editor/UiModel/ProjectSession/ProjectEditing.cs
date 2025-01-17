#nullable enable
using T3.Editor.Gui.Graph.GraphUiModel;
using T3.Editor.Gui.Graph.Window;

namespace T3.Editor.UiModel.ProjectSession;

internal static class ProjectEditing
{
    internal static GraphComponents? Components => GraphWindow.Focused?.Components;
    internal static IGraphCanvas? FocusedCanvas => GraphWindow.Focused?.GraphCanvas;
}