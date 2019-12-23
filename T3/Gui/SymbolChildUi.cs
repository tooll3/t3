using System;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.Graph;
using T3.Gui.Selection;

namespace T3.Gui
{
    /// <summary>
    /// Properties needed for visual representation of an instance. Should later be moved to gui component.
    /// </summary>
    public class SymbolChildUi : ISelectableNode
    {
        public SymbolChild SymbolChild;
        public Guid Id => SymbolChild.Id;
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = DefaultOpSize;
        public bool IsSelected { get { return SelectionManager.IsNodeSelected(this); } }
        public SymbolUi.Styles Style { get; set; }
        internal static Vector2 DefaultOpSize = new Vector2(110, 25);
    }
}