using System;
using T3.Core.Operator;

namespace T3.Core.Commands
{
    public class AddSymbolChildCommand : ICommand
    {
        public string Name => "Add Symbol Child";
        public bool IsUndoable => true;
        public Guid AddedInstanceId => _addedInstanceId;

        public AddSymbolChildCommand(Symbol compositionOp, Guid symbolIdToAdd)
        {
            _parentSymbolId = compositionOp.Id;
            _addedSymbolId = symbolIdToAdd;
            _addedInstanceId = Guid.NewGuid();
        }

        public virtual void Undo()
        {
            var parentSymbol = SymbolRegistry.Entries[_parentSymbolId];
            parentSymbol.RemoveChild(_addedInstanceId);
        }

        public virtual void Do()
        {
            var parentSymbol = SymbolRegistry.Entries[_parentSymbolId];
            var symbolToAdd = SymbolRegistry.Entries[_addedSymbolId];
            _addedInstanceId = parentSymbol.AddChild(symbolToAdd);
        }

        protected readonly Guid _parentSymbolId;
        protected readonly Guid _addedSymbolId;
        private Guid _addedInstanceId;
    }

}
