using System;
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
            var contextLocalTime = (float)context.LocalTime;
            var contextLocalFxTime = (float)context.LocalFxTime;

            float time = 0;

            switch ((Modes)Mode.GetValue(context))
            {
                case Modes.LocalFxTimeInBars:
                    time = contextLocalFxTime;
                    break;
                case Modes.LocalTimeInBars:
                    time = contextLocalTime;
                    break;
                case Modes.PlaybackTimeInSecs:
                    time = contextLocalTime * 240 / (float)context.Playback.Bpm;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            TimeInBars.Value = time * SpeedFactor.GetValue(context);
            TimeInSecs.Value = (float)context.Playback.TimeInSecs * SpeedFactor.GetValue(context);
        }


        private enum Modes
        {
            LocalFxTimeInBars,
            LocalTimeInBars,
            PlaybackTimeInSecs,
        }
        
        [Input(Guid = "8DA7D58D-10A5-4378-8F44-B98F87EC2697", MappedType = typeof(Modes))]
        public readonly InputSlot<int> Mode = new InputSlot<int>();
        
        
        [Input(Guid = "2d9c040d-5244-40ac-8090-d8d57323487b")]
        public readonly InputSlot<float> SpeedFactor = new InputSlot<float>();
    }
}