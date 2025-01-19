
#nullable enable
using T3.Editor.UiModel.ProjectHandling;

namespace T3.Editor.Gui.Interaction;

internal sealed class ScalableGraphSubCanvas : ScalableCanvas
{
    public ScalableGraphSubCanvas(IScalableCanvas parent)
    {
        Parent = parent;
    }

    protected override IScalableCanvas? Parent { get; }
}

internal sealed class CurrentGraphSubCanvas : ScalableCanvas
{
    public CurrentGraphSubCanvas(Vector2? initialScale = null) : base(initialScale) { }
    protected override IScalableCanvas? Parent => ProjectView.Focused?.GraphCanvas;
}