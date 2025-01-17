#nullable enable
namespace T3.Editor.UiModel.ProjectHandling;

internal static class ProjectManager
{
    internal static ProjectView? Components => ProjectView.Focused;
    internal static IGraphCanvas? FocusedCanvas => ProjectView.Focused?.GraphCanvas;
}