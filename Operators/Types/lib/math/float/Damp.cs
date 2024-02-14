using System;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_af9c5db8_7144_4164_b605_b287aaf71bf6
{
    public class Damp : Instance<Damp>
    {
        [Output(Guid = "aacea92a-c166-46dc-b775-d28baf9820f5", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Result = new();

        private const float MinTimeElapsedBeforeEvaluation = 1 / 1000f;

        public Damp()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var inputValue = Value.GetValue(context);
            var damping = Damping.GetValue(context);

            var currentTime = UseAppRunTime.GetValue(context) ? Playback.RunTimeInSecs : context.LocalFxTime;
            if (Math.Abs(currentTime - _lastEvalTime) < MinTimeElapsedBeforeEvaluation)
                return;
            
            if (context.IntVariables.TryGetValue("__MotionBlurPass", out var motionBlurPass))
            {
                if (motionBlurPass > 0)
                {
                    //Log.Debug($"Skip motion blur pass {motionBlurPass}");
                    return;
                }                
            } 

            _lastEvalTime = currentTime;

            var method = (DampFunctions.Methods)Method.GetValue(context).Clamp(0, 1);
            _dampedValue = DampFunctions.DampenFloat(inputValue, _dampedValue, damping, ref _velocity, method);

            MathUtils.ApplyDefaultIfInvalid(ref _dampedValue, 0);
            MathUtils.ApplyDefaultIfInvalid(ref _velocity, 0);

            Result.Value = _dampedValue;
        }

        private float _dampedValue;
        private float _velocity;
        private double _lastEvalTime;
        
        [Input(Guid = "795aca79-dd10-4f28-a290-a30e7b27b436")]
        public readonly InputSlot<float> Value = new();

        [Input(Guid = "F29D5426-5E31-4C7C-BE77-5E45BFB9DAA9")]
        public readonly InputSlot<float> Damping = new();
        
        [Input(Guid = "76D52DF1-597E-4429-9916-13E6E0D93248", MappedType = typeof(DampFunctions.Methods))]
        public readonly InputSlot<int> Method = new();
        
        [Input(Guid = "8909933C-79A8-4127-987B-7B23940A0052")]
        public readonly InputSlot<bool> UseAppRunTime = new();

    }
}
