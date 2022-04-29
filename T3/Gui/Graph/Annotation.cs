using System;
using System.Numerics;
using T3.Gui;
using T3.Gui.Selection;

namespace t3.Gui.Graph
{
    public class Annotation : ISelectableNode
    {
        public string Title;
        public Color Color;
        public Guid Id { get; set; }
        public Vector2 PosOnCanvas { get; set; }
        public Vector2 Size { get; set; }
        public bool IsSelected => NodeSelection.IsNodeSelected(this);

        public Annotation Clone()
        {
            return new Annotation
                       {
                           Id = Guid.NewGuid(),
                           Title = this.Title,
                           Color = this.Color,
                           PosOnCanvas = this.PosOnCanvas,
                           Size = this.Size
                       };
        }
    }
}