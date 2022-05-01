using System;
using T3.Core.Operator;
using T3.Gui.Commands;

namespace t3.Gui.Commands.Graph
{
    public class ChangeSymbolNamespaceCommand : ICommand
    {
        public string Name => "Change Symbol Namespace";
        public bool IsUndoable => true;

        public ChangeSymbolNamespaceCommand(Symbol symbol)
        {
            _symbolId = symbol.Id;
            NewNamespace = symbol.Namespace;
            _originalNamespace = symbol.Namespace;
        }

        public void Do()
        {
            AssignValue(NewNamespace);
        }

        public void Undo()
        {
            AssignValue(_originalNamespace);
        }

        private void AssignValue(string newNamespace)
        {
            var symbol = SymbolRegistry.Entries[_symbolId];
            symbol.Namespace = newNamespace;
        }

        public string NewNamespace { get; set; }
        private readonly string _originalNamespace;
        private readonly Guid _symbolId;
    }
}