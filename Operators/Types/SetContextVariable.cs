using System;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_2a0c932a_eb81_4a7d_aeac_836a23b0b789
{
    public class SetContextVariable : Instance<SetContextVariable>
    {
        [Output(Guid = "9c0c1734-453e-4f88-b20a-47c7e34b7caa")]
        public readonly Slot<Command> Output = new Slot<Command>();

        public SetContextVariable()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            // string variableName = Variable.GetValue(context);
            // float end = End.GetValue(context);
            // for (float i = 0f; i < end; i += 1.0f)
            // {
            //     context.FloatVariables[variableName] = i;
            //
            //     DirtyFlag.InvalidationRefFrame++;
            //     Command.Invalidate();
            //     Command.GetValue(context);
            // }

            var name = VariableName.GetValue(context);
            var newValue = Value.GetValue(context);
            if (string.IsNullOrEmpty(name))
            {
                Log.Warning($"Can't set variable with invalid name {name}");
                return;
            }
            if (!SubGraph.IsConnected)
            {
                context.FloatVariables[name] = newValue;
            }
            else
            {
                var previous = 0f;
                var hadPreviousValue = context.FloatVariables.TryGetValue(name, out previous);
                context.FloatVariables[name] = newValue;
                SubGraph.GetValue(context);
                if (hadPreviousValue)
                {
                    context.FloatVariables[name] = newValue;
                }
                else
                {
                    context.FloatVariables.Remove(name);
                }
            }
        }

        
        [Input(Guid = "E64D396E-855A-4B20-A8F5-39B4F98E8036")]
        public readonly InputSlot<Command> SubGraph = new InputSlot<Command>();
        
        [Input(Guid = "6EE64D39-855A-4B20-A8F5-39B4F98E8036")]
        public readonly InputSlot<string> VariableName = new InputSlot<string>();
        
        [Input(Guid = "68E31EAA-1481-48F4-B742-5177A241FE6D")]
        public readonly InputSlot<float> Value = new InputSlot<float>();

        

        
    }
}

