using System.Diagnostics;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_32325c5b_53f7_4414_b4dd_a436e45528b0
{
    public class SetCommandTime : Instance<SetCommandTime>
    {
       
        [Output(Guid = "FE01C3B6-72E2-494E-8511-6D50C527463F", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Command> Result = new Slot<Command>();

        
        public SetCommandTime()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var newTime = NewTime.GetValue(context);
            var previousKeyframeTime = context.TimeForKeyframes;
            context.TimeForKeyframes = newTime;

            var previousEffectTime = context.TimeForEffects;
            context.TimeForEffects = newTime;
            
            // Execute subtree
            //SubTree.DirtyFlag.Invalidate();
            Result.Value = SubTree.GetValue(context);
            //Log.Debug($"old:{previousTime} / new:{context.TimeInBars}");
            context.TimeForKeyframes = previousKeyframeTime;
            context.TimeForEffects = previousEffectTime;
            //SubTree.DirtyFlag.Clear();
        }
        
        [Input(Guid = "01F2EEF1-E3C1-49A5-B532-9C12DA8CAAC5")]
        public readonly InputSlot<Command> SubTree = new InputSlot<Command>();
        
        [Input(Guid = "d2c934bb-de5c-449c-adf8-7a2f48082e9c")]
        public readonly InputSlot<float> NewTime = new InputSlot<float>();
        
    }
}