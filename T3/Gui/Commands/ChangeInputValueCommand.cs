using System;
using System.Linq;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;

namespace T3.Gui.Commands
{
    public class ChangeInputValueCommand : ICommand
    {
        public string Name => "Change Input Value";
        public bool IsUndoable => true;

        public ChangeInputValueCommand(Symbol inputParent, Guid symbolChildId, SymbolChild.Input input)
        {
            _inputParentSymbolId = inputParent.Id;
            _childId = symbolChildId;
            _inputId = input.InputDefinition.Id;

            OriginalValue = input.Value.Clone();
            Value = input.Value.Clone();
        }

        public void Undo()
        {
            AssignValue(OriginalValue);
        }

        public void Do()
        {
            AssignValue(Value);
        }

        private void AssignValue(InputValue value)
        {
            var inputParentSymbol = SymbolRegistry.Entries[_inputParentSymbolId];
            var symbolChild = inputParentSymbol.Children.Single(child => child.Id == _childId);
            var input = symbolChild.InputValues[_inputId];
            input.Value.Assign(value);
            input.IsDefault = false;
            
            foreach (var parentInstance in inputParentSymbol.InstancesOfSymbol)
            {
                var instance = parentInstance.Children.Single(child => child.SymbolChildId == symbolChild.Id);
                var inputSlot = instance.Inputs.Single(slot => slot.Id == _inputId);
                inputSlot.DirtyFlag.Invalidate();
            }
        }

        public InputValue OriginalValue { get; set; }
        public InputValue Value { get; set; }

        private readonly Guid _inputParentSymbolId;
        private readonly Guid _childId;
        private readonly Guid _inputId;
    }
}