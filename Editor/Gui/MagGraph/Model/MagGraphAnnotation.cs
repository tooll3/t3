#nullable enable
using T3.Editor.UiModel;
using T3.Editor.UiModel.Selection;

namespace T3.Editor.Gui.MagGraph.Model;

internal sealed class MagGraphAnnotation : ISelectableCanvasObject
{
    public required Annotation Annotation;
    public ISelectableCanvasObject Selectable => Annotation;
    
    public Vector2 PosOnCanvas { get => Annotation.PosOnCanvas; set => Annotation.PosOnCanvas = value; }
    public Vector2 Size { get => Annotation.Size; set => Annotation.Size = value; }

    public Vector2 DampedPosOnCanvas;
    public Vector2 DampedSize;
    
    public Guid Id { get; set; }
    
    public int LastUpdateCycle;
    public bool IsRemoved;
}