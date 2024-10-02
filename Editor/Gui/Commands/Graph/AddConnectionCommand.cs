using System;
using T3.Core.Operator;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Commands.Graph
{
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
            var compositionSymbol = SymbolRegistry.Entries[_compositionSymbolId];
            compositionSymbol.AddConnection(_addedConnection, _multiInputIndex);

            var symbolUi = SymbolUiRegistry.Entries[_compositionSymbolId];
            symbolUi.FlagAsModified();
        }

        public void Undo()
        {
            var compositionSymbol = SymbolRegistry.Entries[_compositionSymbolId];
            compositionSymbol.RemoveConnection(_addedConnection, _multiInputIndex);
            
            var symbolUi = SymbolUiRegistry.Entries[_compositionSymbolId];
            symbolUi.FlagAsModified();
        }

        private readonly Guid _compositionSymbolId;
        private readonly Symbol.Connection _addedConnection;
        private readonly int _multiInputIndex;
    }
}