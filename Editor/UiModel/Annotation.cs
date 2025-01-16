using T3.Core.DataTypes.Vector;
using T3.Editor.Gui.Styling;
using T3.Editor.UiModel.Selection;

namespace T3.Editor.UiModel;

public class Annotation : ISelectableCanvasObject
{
    internal string Title = "Untitled Annotation";
    internal Color Color = UiColors.Gray;
    public Guid Id { get; internal init; }
    public Vector2 PosOnCanvas { get; set; }
    public Vector2 Size { get; set; }

    internal Annotation Clone()
    {
        return new Annotation
                   {
                       Id = Guid.NewGuid(),
                       Title = Title,
                       Color = Color,
                       PosOnCanvas = PosOnCanvas,
                       Size = Size
                   };
    }
}