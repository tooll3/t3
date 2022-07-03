using System;
using System.Diagnostics;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_485af23d_543e_44a7_b29f_693ed9533ab5
{
    public class StopWatch : Instance<StopWatch>
    {
        [Output(Guid = "617afbbc-8199-43c0-b630-4563e65959ef", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Delta = new();
        
        [Output(Guid = "195CDCD3-6F02-471A-96E4-3F44A1D03CC2", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> LastDuration = new();
        
        public StopWatch()
        {
            LastDuration.UpdateAction = Update;
            Delta.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var resetHit = MathUtils.WasTriggered(ResetTrigger.GetValue(context), ref _wasResetTrigger);

            if (resetHit)
            {
                LastDuration.Value = (float)(Playback.RunTimeInSecs - _startTime);
                _startTime = Playback.RunTimeInSecs;
            }
            
            var timeInSecs = (float)(Playback.RunTimeInSecs - _startTime);
            var timeMode = (TimeModes)DurationIn.GetValue(context).Clamp(0,1);
            
            Delta.Value = ConvertTime(timeInSecs, timeMode);
            LastDuration.DirtyFlag.Clear();
        }

        private float ConvertTime(double timeInSecs, TimeModes mode)
        {
            switch (mode)
            {
                case TimeModes.TimeInSecs:
                    return (float)timeInSecs;
                
                case TimeModes.BeatTime:
                default:
                {
                    var bpm = Playback.Current != null ? Playback.Current.Bpm : 120;
                    return (float)(timeInSecs * bpm / 240f);
                }
            }
        }

        private double _startTime;
        private bool _wasResetTrigger;
        //private SendTimeAs _timeMode= SendTimeAs.BeatTime;
        
        private enum TimeModes {
            TimeInSecs,
            BeatTime,
        }
        
        
        [Input(Guid = "38754151-704A-4374-817E-98DFACA62E49")]
        public readonly InputSlot<bool> ResetTrigger = new();
        
        [Input(Guid = "C19343B2-7534-43A9-A9A6-CE9019437C62", MappedType = (typeof(TimeModes)))]
        public readonly InputSlot<int> DurationIn = new();
    }
}