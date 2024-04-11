using System;
using T3.Core.Operator;
using T3.Core.SystemUi;
using T3.Editor.SystemUi;

namespace T3.Editor.Gui.Commands.Graph
{
    public class ChangeSymbolNamespaceCommand : ICommand
    {
        public string Name => "Change Symbol Namespace";
        public bool IsUndoable => true;

        public ChangeSymbolNamespaceCommand(Symbol symbol, string newNamespace, Func<Guid, string, string> changeNamespaceAction)
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
            var reason = _changeNamespaceAction(_symbolId, newNamespace);

            if (!string.IsNullOrWhiteSpace(reason))
                BlockingWindow.Instance.ShowMessageBox(reason, "Could not rename namespace");
        }

        private readonly Guid _symbolId;
        private readonly string _newNamespace;
        private readonly string _originalNamespace;
        private readonly Func<Guid, string, string> _changeNamespaceAction;
    }
}