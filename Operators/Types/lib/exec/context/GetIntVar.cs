using System;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_470db771_c7f2_4c52_8897_d3a9b9fc6a4e
{
    public class GetIntVar : Instance<GetIntVar>
    {
        [Output(Guid = "B306B216-630C-4611-90FD-52FF322EBD00")]
        public readonly Slot<int> Result = new Slot<int>();

        public GetIntVar()
        {
            Result.UpdateAction = Update;
            VariableName.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
        }
        
        private void Update(EvaluationContext context)
        {
            var variableName = VariableName.GetValue(context);
            if (context.IntVariables.TryGetValue(variableName, out int value))
            {
                Result.Value = value;
                _complainedOnce = false;
            }
            else
            {
                if (_complainedOnce)
                    return;
                
                Log.Debug($"Can't read undefined int {variableName}.");
                _complainedOnce = true;
            }
        }

        private bool _complainedOnce;


        [Input(Guid = "d7662b65-f249-4887-a319-dc2cf7d192f2")]
        public readonly InputSlot<string> VariableName = new();
    }
}
