using System;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
using static T3.Core.Utils.EasingFunctions;

namespace T3.Operators.Types.Id_3cc78396_862b_47fa_925c_eb327f69f651
{
    public class Easings : Instance<Easings>
    {
        [Output(Guid = "4d1e77d8-bfee-4451-9dcf-f187cd82ec26", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Result = new();

        private const float MinTimeElapsedBeforeEvaluation = 1 / 1000f;

        public Easings()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var inputValue = Value.GetValue(context);  // Target value (0 or 1)
            var duration = Duration.GetValue(context); // Duration of the animation in seconds
            var easeMode = Ease.GetValue(context);     // Easing function selector
            

            var currentTime = UseAppRunTime.GetValue(context) ? Playback.RunTimeInSecs : context.LocalFxTime;
            if (Math.Abs(currentTime - _lastEvalTime) < MinTimeElapsedBeforeEvaluation)
                return;

            if (context.IntVariables.TryGetValue("__MotionBlurPass", out var motionBlurPass) && motionBlurPass > 0)
            {
                return;
            }

            _lastEvalTime = currentTime;

            // Check if input has changed to trigger new animation
            if (Math.Abs(inputValue - _previousInputValue) > 0.001f)
            {
                _startTime = currentTime;
                _initialValue = Result.Value;
                _targetValue = inputValue;
            }

            // Calculate progress based on elapsed time and duration
            var elapsedTime = (float)(currentTime - _startTime);
            var progress = Math.Clamp(elapsedTime / duration, 0f, 1f);

            // Apply selected easing function based on easeMode
            var easedProgress = easeMode switch
            {
                1 => EasingFunctions.InOutSine(progress),
                2 => EasingFunctions.InOutQuad(progress),
                3 => EasingFunctions.InOutCubic(progress),
                4 => EasingFunctions.InOutQuart(progress),
                5 => EasingFunctions.InOutQuint(progress),
                6 => EasingFunctions.InOutExpo(progress),
                7 => EasingFunctions.InOutCirc(progress),
                8 => EasingFunctions.InOutBack(progress),
                9 => EasingFunctions.InOutElastic(progress),
                10 => EasingFunctions.InOutBounce(progress),
                11 => EasingFunctions.OutSine(progress),
                12 => EasingFunctions.OutQuad(progress),
                13 => EasingFunctions.OutCubic(progress),
                14 => EasingFunctions.OutQuart(progress),
                15 => EasingFunctions.OutQuint(progress),
                16 => EasingFunctions.OutExpo(progress),
                17 => EasingFunctions.OutCirc(progress),
                18 => EasingFunctions.OutBack(progress),
                19 => EasingFunctions.OutElastic(progress),
                20 => EasingFunctions.OutBounce(progress),
                21 => EasingFunctions.InSine(progress),
                22 => EasingFunctions.InQuad(progress),
                23 => EasingFunctions.InCubic(progress),
                24 => EasingFunctions.InQuart(progress),
                25 => EasingFunctions.InQuint(progress),
                26 => EasingFunctions.InExpo(progress),
                27 => EasingFunctions.InCirc(progress),
                28 => EasingFunctions.InBack(progress),
                29 => EasingFunctions.InElastic(progress),
                30 => EasingFunctions.InBounce(progress),

                _ => progress,// Default to linear if easeMode is unrecognized or set to 0
            };
            Result.Value = MathUtils.Lerp(_initialValue, _targetValue, easedProgress);
            _previousInputValue = inputValue;
        }

        private double _lastEvalTime;
        private double _startTime;
        private float _initialValue;
        private float _targetValue;
        private float _previousInputValue;

        [Input(Guid = "c2107af4-7b4f-43ef-97fe-934833790032")]
        public readonly InputSlot<float> Value = new();

        [Input(Guid = "da40ddcd-cef8-494a-a7f8-cd8ddfbd7603")]
        public readonly InputSlot<float> Duration = new();

        [Input(Guid = "5580eee8-db7b-4e9b-898d-5c7680b0c302")]
        public readonly InputSlot<bool> UseAppRunTime = new();

        [Input(Guid = "bc388ea3-e1e6-4773-95e5-b8a649c3344f", MappedType = typeof(EasingType))]
        public readonly InputSlot<int> Ease = new();
        
    }
}
