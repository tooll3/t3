using System;
using System.Collections.Generic;
using System.Numerics;

namespace T3.Editor.Gui.Selection
{
    public interface ISelectableCanvasObject
    {
        Guid Id { get; }
        Vector2 PosOnCanvas { get; set; }
        Vector2 Size { get; set; }
        bool IsSelected { get; }
    }

    public interface ISelectionContainer
    {
        IEnumerable<ISelectableCanvasObject> GetSelectables();
    }
}
