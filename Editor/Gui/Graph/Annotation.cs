using System;
using System.Numerics;
using T3.Core.DataTypes.Vector;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.Graph
{
    public class Annotation : ISelectableCanvasObject
    {
        public string Title = "Untitled Annotation";
        public Color Color = UiColors.Gray;
        public Guid Id { get; init; }
        public Vector2 PosOnCanvas { get; set; }
        public Vector2 Size { get; set; }
        public bool IsSelected => NodeSelection.IsNodeSelected(this);

        public Annotation Clone()
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
}