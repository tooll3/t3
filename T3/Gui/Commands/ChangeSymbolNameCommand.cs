using System;
using System.Linq;
using T3.Core.Operator;

namespace T3.Gui.Commands
{
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

        public virtual void Do()
        {
            AssignValue(NewName);
        }

        public virtual void Undo()
        {
            AssignValue(_originalName);
        }


        private void AssignValue(string newName)
        {
            var symbol = SymbolRegistry.Entries[_symbolId];
            symbol.Name = newName;
        }

        public string NewName { get; set; }
        private readonly string _originalName;

        private readonly Guid _symbolId;
    }
}