using System.Numerics;
using T3.Gui.Selection;

namespace T3.Core.Operator
{
    /// <summary>
    /// Properties needed for visual representation of an instance. Should later be moved to gui component.
    /// </summary>
    public class SymbolChildUi : ISelectable
    {
        public SymbolChild SymbolChild;
        public Vector2 Position { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = new Vector2(100, 30);
        public bool IsVisible { get; set; } = true;
        public bool IsSelected { get; set; } = false;
        public string Name { get; set; } = string.Empty;
        public string ReadableName => string.IsNullOrEmpty(Name) ? SymbolChild.Symbol.SymbolName : Name;

    }
}