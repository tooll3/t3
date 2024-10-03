using System.Collections.Generic;

namespace T3.Editor.Gui.Windows.ResearchCanvas.SnapGraph;

public class SnapGroup
{
    public List<SnapGraphItem.OutputLine> FreeOutSockets = new();  // Todo: Implement
    public List<SnapGraphItem.InputLine> FreeInSockets = new(); // Todo: Implement
    public readonly List<SnapGraphItem> Items = new(8);
    public List<int> ConnectionUiIndices = new(8);  
    //public List<ConnectionUi> SnappedConnections = new();
}