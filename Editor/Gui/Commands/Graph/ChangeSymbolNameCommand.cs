using T3.Core.Operator;

namespace T3.Editor.Gui.Commands.Graph;

public class ChangeSymbolNameCommand : ICommand
{
    public string Name => "Change Symbol Name";
    public bool IsUndoable => true;

    public ChangeSymbolNameCommand(Symbol symbol)
    {
        _symbolId = symbol.Id;
        NewName = symbol.Name;
        _originalName = symbol.Name;
    }

    public void Do()
    {
        AssignValue(NewName);
    }

    public void Undo()
    {
        AssignValue(_originalName);
    }

    private void AssignValue(string newName)
    {
        throw new NotImplementedException("Not implemented yet");
    }

    public string NewName { get; set; }
    private readonly string _originalName;
    private readonly Guid _symbolId;
}