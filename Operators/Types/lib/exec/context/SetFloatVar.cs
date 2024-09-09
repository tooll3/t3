using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_2a0c932a_eb81_4a7d_aeac_836a23b0b789
{
    public class SetFloatVar : Instance<SetFloatVar>
    {
        [Output(Guid = "9c0c1734-453e-4f88-b20a-47c7e34b7caa")]
        public readonly Slot<Command> Output = new();

        public SetFloatVar()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var name = VariableName.GetValue(context);
            var newValue = Value.GetValue(context);
            var clearAfterExecution = ClearAfterExecution.GetValue(context);
            
            if (string.IsNullOrEmpty(name))
            {
                Log.Warning($"Can't set variable with invalid name {name}", this);
                return;
            }

            if (SubGraph.HasInputConnections)
            {
                var hadPreviousValue = context.FloatVariables.TryGetValue(name, out var previous);
                context.FloatVariables[name] = newValue;

                SubGraph.GetValue(context);

                if (hadPreviousValue)
                {
                    context.FloatVariables[name] = previous;
                }
                else if(!clearAfterExecution)
                {
                    context.FloatVariables.Remove(name);
                }
            }
            else
            {
                context.FloatVariables[name] = newValue;
            }
        }
        
        
        [Input(Guid = "E64D396E-855A-4B20-A8F5-39B4F98E8036")]
        public readonly InputSlot<Command> SubGraph = new();
        
        [Input(Guid = "6EE64D39-855A-4B20-A8F5-39B4F98E8036")]
        public readonly InputSlot<string> VariableName = new();
        
        [Input(Guid = "68E31EAA-1481-48F4-B742-5177A241FE6D")]
        public readonly InputSlot<float> Value = new();

        [Input(Guid = "DA431996-4C4C-4CDC-9723-9116BBB5440C")]
        public readonly InputSlot<bool> ClearAfterExecution = new ();
        

        
    }
}

