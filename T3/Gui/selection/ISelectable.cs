using System.Numerics;

namespace T3.Gui.Selection
{
    public interface ISelectable
    {
        Vector2 Position { get; set; }
        Vector2 Size { get; }
        bool IsSelected { get; set; }
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
    }

    public interface IConnectionSource : IConnectable
    {
    }
}
