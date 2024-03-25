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

        private readonly string _creationStack;

        public ResetInputToDefault(Symbol symbol, Guid symbolChildId, SymbolChild.Input input)
        {
            _inputParentSymbolId = symbol.Id;
            _childId = symbolChildId;
            _inputId = input.InputDefinition.Id;

            OriginalValue = input.Value.Clone();
            _wasDefault = input.IsDefault;
            _creationStack = Environment.StackTrace;
        }

        public void Undo()
        {
            try
            {
                AssignValue(_wasDefault);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to undo ResetInputToDefault command: {e.Message}\nCommand created at: {_creationStack}");
            }
        }

        public void Do()
        {
            try
            {
                AssignValue(true);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to execute ResetInputToDefault command: {e.Message}\nCommand created at: {_creationStack}\n");
            }
        }

        private void AssignValue(bool shouldBeDefault)
        {
            var inputParentSymbol = SymbolRegistry.Entries[_inputParentSymbolId];
            var instances = inputParentSymbol.InstancesOfSymbol;
            var thisInstance = instances.SingleOrDefault(child => child.SymbolChildId == _childId);
            
            if(thisInstance == null)
                throw new InvalidOperationException($"Instance not found: sequence contains {instances.Count(c => c.SymbolChildId == _childId)} elements with id {_childId}");
            
            var symbolChild = thisInstance.SymbolChild;
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
            foreach (var instance in inputParentSymbol.InstancesOfSymbol)
            {
                var inputSlot = instance.Inputs.Single(slot => slot.Id == _inputId);
                inputSlot.DirtyFlag.ForceInvalidate();
            }
        }

        private InputValue OriginalValue { get; set; }
        private readonly bool _wasDefault;

        private readonly Guid _inputParentSymbolId;
        private readonly Guid _childId;
        private readonly Guid _inputId;
    }
}