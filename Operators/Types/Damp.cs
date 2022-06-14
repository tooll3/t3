using System;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_af9c5db8_7144_4164_b605_b287aaf71bf6
{
    public class Damp : Instance<Damp>
    {
        [Output(Guid = "aacea92a-c166-46dc-b775-d28baf9820f5", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Result = new Slot<float>();

        public Damp()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var v = Value.GetValue(context);
            var damping = Damping.GetValue(context);

            var t = context.LocalFxTime;
            if (Math.Abs(t - _lastEvalTime) < 0.001f)
                return;
            
            _lastEvalTime = t;
            const float FrameRate = 60;

            var framesPassed = (int)((Playback.LastFrameDuration * FrameRate) - 0.5f).Clamp(0,5) + 1 ;
            
            Log.Debug("Frame count: " + framesPassed);
            for (int stepIndex = 0; stepIndex < framesPassed; stepIndex++)
            {
                _dampedValue = MathUtils.Lerp(v,_dampedValue, damping);
            }

            // Prevent NaN
            if (!float.IsNormal(_dampedValue))
            {
                _dampedValue = 0;
            }
            
            Result.Value = _dampedValue;
        }

        private float _dampedValue;
        private double _lastEvalTime; 
        
        [Input(Guid = "795aca79-dd10-4f28-a290-a30e7b27b436")]
        public readonly InputSlot<float> Value = new InputSlot<float>();

        [Input(Guid = "F29D5426-5E31-4C7C-BE77-5E45BFB9DAA9")]
        public readonly InputSlot<float> Damping = new InputSlot<float>();
        
    }
}
