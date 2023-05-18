using System;
using System.Linq;
using T3.Core.Logging;

namespace T3.Editor.Gui.Commands.Graph
{
    public class ChangeInstanceBypassedCommand : ICommand
    {
        public string Name => "Bypass";
        public bool IsUndoable => true;

        public ChangeInstanceBypassedCommand(SymbolChildUi symbolChildUi, bool setBypassedTo)
        {
            _inputParentSymbolId = symbolChildUi.SymbolChild.Parent.Id;
            _childId = symbolChildUi.Id;
            _originalState = symbolChildUi.IsBypassed;
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
            if (!SymbolUiRegistry.Entries.TryGetValue(_inputParentSymbolId, out var symbolUi))
                return;
            
            var childUi = symbolUi.ChildUis.SingleOrDefault(c => c.Id == _childId);
            if (childUi == null)
            {
                Log.Assert("Failed to find childUi");
                return;
            }

            childUi.IsBypassed = shouldBeBypassed;
        }

        private readonly bool _newState;
        private readonly bool _originalState;
        private readonly Guid _inputParentSymbolId;
        private readonly Guid _childId;
    }
}