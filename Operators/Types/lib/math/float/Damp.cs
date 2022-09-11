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
            const float FrameRate = 60f;

            var method = (Methods)Method.GetValue(context).Clamp(0, 1);
            switch (method)
            {
                case Methods.LinearInterpolation:
                    var framesPassed = (int)((Playback.LastFrameDuration * FrameRate) - 0.5f).Clamp(0,5) + 1 ;
                    
                    for (int stepIndex = 0; stepIndex < framesPassed; stepIndex++)
                    {
                        _dampedValue = MathUtils.Lerp(v,_dampedValue, damping);
                    }

                    break;
                
                case Methods.DampedSpring:
                    _dampedValue = MathUtils.SpringDamp(v,_dampedValue , ref _velocity, 0.5f/(damping + 0.001f),  (float)Playback.LastFrameDuration);
                    break;
            }
            
            // Prevent NaN
            if (float.IsNaN(_dampedValue) || float.IsNaN(_velocity))
            {
                _dampedValue = 0;
                _velocity = 0;
            }
            
            Result.Value = _dampedValue;
        }

        private enum Methods
        {
            LinearInterpolation,
            DampedSpring
        }
        
        private float _dampedValue;
        private float _velocity;
        private double _lastEvalTime;
        
        [Input(Guid = "795aca79-dd10-4f28-a290-a30e7b27b436")]
        public readonly InputSlot<float> Value = new();

        [Input(Guid = "F29D5426-5E31-4C7C-BE77-5E45BFB9DAA9")]
        public readonly InputSlot<float> Damping = new();
        
        [Input(Guid = "76D52DF1-597E-4429-9916-13E6E0D93248", MappedType = typeof(Methods))]
        public readonly InputSlot<int> Method = new();
        
    }
}
