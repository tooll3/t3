using System;
using T3.Core.Operator;

namespace T3.Editor.Gui.Commands.Graph
{
    public class ChangeSymbolNamespaceCommand : ICommand
    {
        public string Name => "Change Symbol Namespace";
        public bool IsUndoable => true;

        public ChangeSymbolNamespaceCommand(Symbol symbol, string newNamespace, Action<Guid, string> changeNamespaceAction)
        {
            _newNamespace = newNamespace;
            _symbolId = symbol.Id;
            _originalNamespace = symbol.Namespace;
            _changeNamespaceAction = changeNamespaceAction;
        }

        public void Do()
        {
            AssignValue(_newNamespace);
        }

        public void Undo()
        {
            AssignValue(_originalNamespace);
        }

        private void AssignValue(string newNamespace)
        {
            _changeNamespaceAction(_symbolId, newNamespace);
        }

        private readonly Guid _symbolId;
        private readonly string _newNamespace;
        private readonly string _originalNamespace;
        private readonly Action<Guid, string> _changeNamespaceAction;
    }
}