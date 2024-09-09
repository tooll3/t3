using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3036067a_a4c2_434b_b0e3_ac95c5c943f4
{
    public class TimeClip : Instance<TimeClip>
    {
        [Output(Guid = "de6ff8b5-40fe-47fa-b9f2-d926b17f9a7f")]
        public readonly TimeClipSlot<Command> Output = new();
        
        public TimeClip()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var startTime = Output.TimeClip.TimeRange.Start;
            var endTime = Output.TimeClip.TimeRange.End;
            var normalizedTime = (context.LocalFxTime - startTime) / (endTime - startTime);
            context.FloatVariables["_normalizedTime"] = (float)normalizedTime;
            
            var commands = Command.GetCollectedTypedInputs();

            // do preparation if needed
            for (int i = 0; i < commands.Count; i++)
            {
                commands[i].Value?.PrepareAction?.Invoke(context);
            }

            // execute commands
            for (int i = 0; i < commands.Count; i++)
            {
                commands[i].GetValue(context);
            }

            // cleanup after usage
            for (int i = 0; i < commands.Count; i++)
            {
                commands[i].Value?.RestoreAction?.Invoke(context);
            }                
            

            Command.DirtyFlag.Clear();
        }

        [Input(Guid = "35f501f4-5c79-4628-9441-8b3782544bf6")]
        public readonly MultiInputSlot<Command> Command = new();
        
    }
}