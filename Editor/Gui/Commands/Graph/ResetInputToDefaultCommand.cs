using System;
using System.Linq;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace T3.Editor.Gui.Commands.Graph
{
    public class ResetInputToDefault : ICommand
    {
        public string Name => "Reset Input Value to default";
        public bool IsUndoable => true;

        public ResetInputToDefault(Symbol inputParent, Guid symbolChildId, SymbolChild.Input input)
        {
            _inputParentSymbolId = inputParent.Id;
            _childId = symbolChildId;
            _inputId = input.InputDefinition.Id;

            OriginalValue = input.Value.Clone();
            _wasDefault = input.IsDefault;
        }

        public void Undo()
        {
            AssignValue(_wasDefault);
        }

        public void Do()
        {
            AssignValue(true);
        }

        private void AssignValue(bool shouldBeDefault)
        {
            var inputParentSymbol = SymbolRegistry.Entries[_inputParentSymbolId];
            var symbolChild = inputParentSymbol.Children.Single(child => child.Id == _childId);
            var input = symbolChild.Inputs[_inputId];

            if (shouldBeDefault)
            {
                //input.IsDefault = true;
                input.ResetToDefault();
            }
            else
            {
                input.Value.Assign(OriginalValue);
                input.IsDefault = false;
            }

            //inputParentSymbol.InvalidateInputInAllChildInstances(input);
            foreach (var parentInstance in inputParentSymbol.InstancesOfSymbol)
            {
                var instance = parentInstance.Children.Single(child => child.SymbolChildId == symbolChild.Id);
                var inputSlot = instance.Inputs.Single(slot => slot.Id == _inputId);
                inputSlot.DirtyFlag.Invalidate(true);
            }
        }

        private InputValue OriginalValue { get; set; }
        private readonly bool _wasDefault;

        private readonly Guid _inputParentSymbolId;
        private readonly Guid _childId;
        private readonly Guid _inputId;
    }
}