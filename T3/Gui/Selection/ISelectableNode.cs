using System;
using System.Numerics;

namespace T3.Gui.Selection
{
    public interface ISelectableNode
    {
        Guid Id { get; }
        Vector2 PosOnCanvas { get; set; }
        Vector2 Size { get; set; }
        bool IsSelected { get; set; }
    }
}
