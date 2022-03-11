using System;
using System.Numerics;
using T3.Gui.Selection;

namespace T3.Gui
{
    public class Annotation : ISelectableNode
    {
        public string Title;
        public string Description;
        public Color Color;
        public Guid Id { get; set; }
        public Vector2 PosOnCanvas { get; set; }
        public Vector2 Size { get; set; }
        public bool IsSelected => SelectionManager.IsNodeSelected(this);

        public Annotation Clone()
        {
            return new Annotation
                       {
                           Id = Guid.NewGuid(),
                           Title = this.Title,
                           Description = this.Description,
                           Color = this.Color,
                           PosOnCanvas = this.PosOnCanvas,
                           Size = this.Size
                       };
        }
    }
}