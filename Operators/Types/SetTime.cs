using System.Diagnostics;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_d3dbc1cf_0642_4e36_85eb_95bd5a2950b5
{
    public class SetTime : Instance<SetTime>
    {
       
        [Output(Guid = "46046E85-D4DE-4A24-8CDF-1945755D0F88", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Result = new Slot<float>();

        
        public SetTime()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var previousKeyframeTime = context.TimeForKeyframes;
            var previousEffectTime = context.TimeForEffects;
            
            var newTime = NewTime.GetValue(context);
            context.TimeForKeyframes = newTime;
            context.TimeForEffects = newTime;
            
            // Execute subtree
            Result.Value = SubTree.GetValue(context);
            context.TimeForKeyframes = previousKeyframeTime;
            context.TimeForEffects = previousEffectTime;
        }
        
        [Input(Guid = "6220E7DC-7AED-48B8-B419-29393988BBBE")]
        public readonly InputSlot<float> SubTree = new InputSlot<float>();
        
        [Input(Guid = "CD3C6854-29DE-4C52-9ED8-7BA8D847FC34")]
        public readonly InputSlot<float> NewTime = new InputSlot<float>();
        
    }
}