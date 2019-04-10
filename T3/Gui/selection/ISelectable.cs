// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.Numerics;

namespace T3.Gui.Selection
{
    public interface ISelectable
    {
        Vector2 Position { get; set; }
        Vector2 Size { get; set; }
        bool IsSelected { get; set; }
    }
}
