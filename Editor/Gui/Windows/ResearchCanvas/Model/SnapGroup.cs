using System.Collections.Generic;

namespace T3.Editor.Gui.Windows.ResearchCanvas.Model;

public class SnapGroup
{
    public List<SnapGraphItem.OutSocket> FreeOutSockets = new();  // Todo: Implement
    public List<SnapGraphItem.InSocket> FreeInSockets = new(); // Todo: Implement
    public readonly List<SnapGraphItem> Items = new(8);
    public List<int> ConnectionUiIndices = new(8);  
    //public List<ConnectionUi> SnappedConnections = new();
}