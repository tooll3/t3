using System;
using T3.Core.Operator;

namespace T3.Gui.Commands
{
    public class RemoveConnectionCommand : ICommand
    {
        public string Name => "Remove Connection";
        public bool IsUndoable => true;

        public RemoveConnectionCommand(Symbol compositionSymbol, Symbol.Connection connectionToRemove, int multiInputIndex)
        {
            _removedConnection = connectionToRemove;
            _compositionSymbolId = compositionSymbol.Id;
            _multiInputIndex = multiInputIndex;
        }

        public void Do()
        {
            var compositionSymbol = SymbolRegistry.Entries[_compositionSymbolId];
            compositionSymbol.RemoveConnection(_removedConnection, _multiInputIndex);
        }

        public void Undo()
        {
            var compositionSymbol = SymbolRegistry.Entries[_compositionSymbolId];
            compositionSymbol.AddConnection(_removedConnection, _multiInputIndex);
        }
        
        private readonly Guid _compositionSymbolId;
        private readonly Symbol.Connection _removedConnection;
        private readonly int _multiInputIndex;
    }
}