using System.Diagnostics;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9cb4d49e_135b_400b_a035_2b02c5ea6a72
{
    public class Time : Instance<Time>
    {
        [Output(Guid = "b20573fe-7a7e-48e1-9370-744288ca6e32", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> TimeInBars = new Slot<float>();

        [Output(Guid = "A606B326-F3AF-470B-B6E5-3175F7A54E31", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> TimeInSecs = new Slot<float>();

        
        public Time()
        {
            TimeInBars.UpdateAction = Update;
            TimeInSecs.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var contextTimeForKeyframes = (float)context.TimeForKeyframes;
            var contextTimeForEffects = (float)context.TimeForEffects;

            var time = (Modes)Mode.GetValue(context) == Modes.EffectTimeInBars
                           ? contextTimeForEffects
                           : contextTimeForKeyframes;
            
            TimeInBars.Value = time * SpeedFactor.GetValue(context);
            TimeInSecs.Value = (float)EvaluationContext.GlobalTimeInSecs * SpeedFactor.GetValue(context);
        }


        private enum Modes
        {
            EffectTimeInBars,
            KeyframeTimeInBars,
        }
        
        [Input(Guid = "8DA7D58D-10A5-4378-8F44-B98F87EC2697", MappedType = typeof(Modes))]
        public readonly InputSlot<int> Mode = new InputSlot<int>();
        
        
        [Input(Guid = "2d9c040d-5244-40ac-8090-d8d57323487b")]
        public readonly InputSlot<float> SpeedFactor = new InputSlot<float>();
    }
}