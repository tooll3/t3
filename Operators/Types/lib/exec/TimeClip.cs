using T3.Core.DataTypes;
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
            Command.GetValue(context); 
        }

        [Input(Guid = "35f501f4-5c79-4628-9441-8b3782544bf6")]
        public readonly InputSlot<Command> Command = new();
        
    }
}