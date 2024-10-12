using T3.Core.Operator;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Commands.Graph;

public class AddConnectionCommand : ICommand
{
    public string Name => "Add Connection";
    public bool IsUndoable => true;

    public AddConnectionCommand(Symbol compositionSymbol, Symbol.Connection connectionToAdd, int multiInputIndex)
    {
        _addedConnection = connectionToAdd;
        _compositionSymbolId = compositionSymbol.Id;
        _multiInputIndex = multiInputIndex;
    }

    public void Do()
    {
        if (!SymbolUiRegistry.TryGetSymbolUi(_compositionSymbolId, out var compositionSymbolUi))
        {
            Log.Warning($"Could not find symbol with id {_compositionSymbolId} - was it removed?");
            return;
        }
            
        compositionSymbolUi!.Symbol.AddConnection(_addedConnection, _multiInputIndex);
        compositionSymbolUi.FlagAsModified();
    }

    public void Undo()
    {
        if (!SymbolUiRegistry.TryGetSymbolUi(_compositionSymbolId, out var compositionSymbolUi))
        {
            Log.Warning($"Could not find symbol with id {_compositionSymbolId} - was it removed?");
            return;
        }
            
        compositionSymbolUi!.Symbol.RemoveConnection(_addedConnection, _multiInputIndex);
        compositionSymbolUi.FlagAsModified();
    }

    private readonly Guid _compositionSymbolId;
    private readonly Symbol.Connection _addedConnection;
    private readonly int _multiInputIndex;
}