// Copyright (c) 2016 Framefield. All rights reserved.
// Released under the MIT license. (see LICENSE.txt)

using System.Collections.Generic;
using System.Numerics;
using T3.Gui.Graph;

namespace T3.Gui.Selection
{
    public interface ISelectable
    {
        Vector2 Position { get; set; }
        Vector2 Size { get; set; }
        bool IsSelected { get; set; }
        float GetHorizontalOverlapWith(ISelectable element);
    }

    public interface IConnectable : ISelectable
    {
        //List<IStackable> GetOpsConnectedToInputs();
        //List<IStackable> GetOpsConnectedToOutputs();
        //List<ConnectionLine> GetOutputConnections();
        //List<ConnectionLine> GetInputConnections();
    }

    public interface IConnectionTarget : IConnectable
    {
        List<VisibleInputSlot> GetVisibileInputSlots();
    }

    public interface IConnectionSource : IConnectable
    {
    }

    public interface IStackable : IConnectable
    {
        bool IsStackableAbove { get; }
        bool IsStackableBelow { get; }
    }
}
