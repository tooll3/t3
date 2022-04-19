using System;
using System.Diagnostics;
using System.Linq;
using T3.Core.Animation;
using T3.Core.Logging;
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
            _isAnimated = inputParent.Animator.IsAnimated(_childId, _inputId);

            OriginalValue = input.Value.Clone();
            NewValue = input.Value.Clone();
        }

        public void Undo()
        {
            if (_isAnimated)
                Log.Warning("Undo of animation currently not supported");
            else
                AssignValue(OriginalValue);
        }

        public void Do()
        {
            AssignValue(NewValue);
        }

        public void AssignValue(InputValue value)
        {
            var inputParentSymbol = SymbolRegistry.Entries[_inputParentSymbolId];
            var symbolChild = inputParentSymbol.Children.Single(child => child.Id == _childId);



            if (_isAnimated)
            {
                NewValue.Assign(value);
                var symbolUi = SymbolUiRegistry.Entries[symbolChild.Symbol.Id];
                var inputUi = symbolUi.InputUis[_inputId];
                var animator = inputParentSymbol.Animator;

                foreach (var parentInstance in inputParentSymbol.InstancesOfSymbol)
                {
                    var instance = parentInstance.Children.Single(child => child.SymbolChildId == symbolChild.Id);
                    var inputSlot = instance.Inputs.Single(slot => slot.Id == _inputId);
                    inputUi.ApplyValueToAnimation(inputSlot, NewValue, animator);
                    inputSlot.DirtyFlag.Invalidate(true);
                }
            }
            else
            {
                var input = symbolChild.InputValues[_inputId];
                input.Value.Assign(value);
                input.IsDefault = false;
                
                foreach (var parentInstance in inputParentSymbol.InstancesOfSymbol)
                {
                    var instance = parentInstance.Children.Single(child => child.SymbolChildId == symbolChild.Id);
                    var inputSlot = instance.Inputs.Single(slot => slot.Id == _inputId);
                    inputSlot.DirtyFlag.Invalidate(true);
                }
            }
        }
        

        public InputValue OriginalValue { get; set; }
        public InputValue NewValue { get; set; }

        private readonly Guid _inputParentSymbolId;
        private readonly Guid _childId;
        private readonly Guid _inputId;
        private bool _isAnimated;
    }
}