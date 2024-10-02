using System;
using System.Linq;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Editor.Gui.Commands.Graph
{
    public class ChangeInstanceBypassedCommand : ICommand
    {
        public string Name => "Bypass";
        public bool IsUndoable => true;

        public ChangeInstanceBypassedCommand(SymbolChild symbolChild, bool setBypassedTo)
        {
            _inputParentSymbolId = symbolChild.Parent.Id;
            _childId = symbolChild.Id;
            _originalState = symbolChild.IsBypassed;
            _newState = setBypassedTo;
        }

        public void Undo()
        {
            AssignValue(_originalState);
        }

        public void Do()
        {
            AssignValue(_newState);
        }

        private void AssignValue(bool shouldBeBypassed)
        {
            if (!SymbolRegistry.Entries.TryGetValue(_inputParentSymbolId, out var symbol))
                return;
            
            var child = symbol.Children.SingleOrDefault(c => c.Id == _childId);
            if (child == null)
            {
                Log.Assert("Failed to find child");
                return;
            }

            child.IsBypassed = shouldBeBypassed;
        }

        private readonly bool _newState;
        private readonly bool _originalState;
        private readonly Guid _inputParentSymbolId;
        private readonly Guid _childId;
    }
}