using System.Numerics;
using T3.Core.Operator;
using T3.Gui.Graph;
using T3.Gui.Selection;

namespace T3.Gui
{
    /// <summary>
    /// Properties needed for visual representation of an instance. Should later be moved to gui component.
    /// </summary>
    public class SymbolChildUi : ISelectable
    {
        public SymbolChild SymbolChild;
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = GraphCanvas.DefaultOpSize;
        public bool IsVisible { get; set; } = true;
        public bool IsSelected { get; set; } = false;

    }
}