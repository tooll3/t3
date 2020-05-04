using System;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Gui.ChildUi;
using T3.Gui.Graph;
using T3.Gui.Selection;
using UiHelpers;

namespace T3.Gui
{
    /// <summary>
    /// Properties needed for visual representation of an instance. Should later be moved to gui component.
    /// </summary>
    public class SymbolChildUi : ISelectableNode
    {
        public enum Styles
        {
            Default,
            Expanded,
            Resizable,
            WithThumbnail,
        }
        
        internal static Vector2 DefaultOpSize { get; } = new Vector2(110, 25);
        
        public SymbolChild SymbolChild;
        public Guid Id => SymbolChild.Id;
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = DefaultOpSize;
        public bool IsSelected => SelectionManager.IsNodeSelected(this);
        public Styles Style { get; set; }

        public bool DrawCustomUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect)
        {
            if (!CustomChildUiRegistry.Entries.TryGetValue(instance.Type, out var drawFunction))
                return false;

            return drawFunction(instance, drawList, selectableScreenRect);
        }

        public virtual SymbolChildUi Clone()
        {
            return new SymbolChildUi()
                   {
                       PosOnCanvas = PosOnCanvas,
                       Size = Size,
                       Style = Style,
                       SymbolChild = SymbolChild,
                   };
        }
    }
}