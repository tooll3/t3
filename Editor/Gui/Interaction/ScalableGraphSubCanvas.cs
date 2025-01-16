
#nullable enable
using T3.Editor.Gui.Graph.Legacy;

namespace T3.Editor.Gui.Interaction;

internal sealed class ScalableGraphSubCanvas : ScalableCanvas
{
    public ScalableGraphSubCanvas(IScalableCanvas parent)
    {
        Parent = parent;
    }
    public override IScalableCanvas? Parent { get; }
}

internal sealed class CurrentGraphSubCanvas : ScalableCanvas
{
    public CurrentGraphSubCanvas(Vector2? initialScale = null) : base(initialScale) { }
    public override IScalableCanvas? Parent => GraphWindow.Focused?.GraphCanvas;
}