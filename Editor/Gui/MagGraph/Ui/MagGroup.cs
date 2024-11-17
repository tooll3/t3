namespace T3.Editor.Gui.MagGraph.Ui;

internal abstract class MagGroup
{
    public List<MagGraphItem.OutputLine> FreeOutSockets = new();  // Todo: Implement
    public List<MagGraphItem.InputLine> FreeInSockets = new(); // Todo: Implement
    public readonly List<MagGraphItem> Items = new(8);
    public List<int> ConnectionUiIndices = new(8);  
    //public List<ConnectionUi> SnappedConnections = new();
}