#nullable enable
using T3.Editor.Gui.Interaction.Snapping;
using T3.Editor.UiModel;
using T3.Editor.UiModel.Selection;

namespace T3.Editor.Gui.MagGraph.Model;

/// <summary>
/// A wrapper to <see cref="Annotation"/> to provide damping other potential other features of the mag graph UI.
/// </summary>
internal sealed class MagGraphAnnotation : ISelectableCanvasObject, IValueSnapAttractor
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
    
    SnapResult? IValueSnapAttractor.CheckForSnap(double value, float canvasScale, IValueSnapAttractor.Orientation orientation)
    {
        SnapResult? bestSnapResult = null;

        ValueSnapHandler.CheckForBetterSnapping(value, DampedPosOnCanvas.X, canvasScale, ref bestSnapResult);
        ValueSnapHandler.CheckForBetterSnapping(value, DampedPosOnCanvas.X + DampedSize.X, canvasScale, ref bestSnapResult);
        
        return bestSnapResult;
    }
}