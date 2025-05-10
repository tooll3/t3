#nullable enable
using T3.Editor.UiModel;
using T3.Editor.UiModel.Selection;

namespace T3.Editor.Gui.MagGraph.Model;

/// <summary>
/// A wrapper to <see cref="Annotation"/> to provide damping other potential other features of the mag graph UI.
/// </summary>
internal sealed class MagGraphAnnotation : ISelectableCanvasObject
{
    public required Annotation Annotation;
    public ISelectableCanvasObject Selectable => Annotation;
    
    public Vector2 PosOnCanvas { get => Annotation.PosOnCanvas; set => Annotation.PosOnCanvas = value; }
    public Vector2 Size { get => Annotation.Size; set => Annotation.Size = value; }

    public Vector2 DampedPosOnCanvas;
    public Vector2 DampedSize;
    
    public Guid Id { get; init; }
    
    public int LastUpdateCycle;
    public bool IsRemoved;
}