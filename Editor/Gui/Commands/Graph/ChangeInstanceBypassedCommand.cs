using System;
using System.Linq;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.UiModel;

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
            if (!SymbolUiRegistry.TryGetSymbolUi(_inputParentSymbolId, out var symbolUi))
                return;
            
            var symbol = symbolUi.Symbol;
            if (!symbol.Children.TryGetValue(_childId, out var child))
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