using System;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
using static T3.Core.Utils.EasingFunctions;

namespace T3.Operators.Types.Id_3cc78396_862b_47fa_925c_eb327f69f651
{
    public class Ease : Instance<Ease>
    {
        [Output(Guid = "4d1e77d8-bfee-4451-9dcf-f187cd82ec26", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Result = new();

        private const float MinTimeElapsedBeforeEvaluation = 1 / 1000f;

        public Ease()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var inputValue = Value.GetValue(context);  // Target value (0 or 1)
            var duration = Duration.GetValue(context); // Duration of the animation in seconds
            var easeMode = Mode.GetValue(context);     // Easing function selector
            var easeDirection = (EaseDirection)Direction.GetValue(context);


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

            var easedProgress = progress;
            switch (easeDirection)
            {
                case EaseDirection.In:
                    easedProgress = easeMode switch
                    {
                        1 => EasingFunctions.InSine(progress),
                        2 => EasingFunctions.InQuad(progress),
                        3 => EasingFunctions.InCubic(progress),
                        4 => EasingFunctions.InQuart(progress),
                        5 => EasingFunctions.InQuint(progress),
                        6 => EasingFunctions.InExpo(progress),
                        7 => EasingFunctions.InCirc(progress),
                        8 => EasingFunctions.InBack(progress),
                        9 => EasingFunctions.InElastic(progress),
                        10 => EasingFunctions.InBounce(progress),
                        _ => progress
                    };
                    break;

                case EaseDirection.Out:
                    easedProgress = easeMode switch
                    {
                        1 => EasingFunctions.OutSine(progress),
                        2 => EasingFunctions.OutQuad(progress),
                        3 => EasingFunctions.OutCubic(progress),
                        4 => EasingFunctions.OutQuart(progress),
                        5 => EasingFunctions.OutQuint(progress),
                        6 => EasingFunctions.OutExpo(progress),
                        7 => EasingFunctions.OutCirc(progress),
                        8 => EasingFunctions.OutBack(progress),
                        9 => EasingFunctions.OutElastic(progress),
                        10 => EasingFunctions.OutBounce(progress),
                        _ => progress
                    };
                    break;

                case EaseDirection.InOut:
                    easedProgress = easeMode switch
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
                        _ => progress
                    };
                    break;
            }
            Result.Value = MathUtils.Lerp(_initialValue, _targetValue, easedProgress);
            _previousInputValue = inputValue;
        }

        private double _lastEvalTime;
        private double _startTime;
        private float _initialValue;
        private float _targetValue;
        private float _previousInputValue;

        [Input(Guid = "c2107af4-7b4f-43ef-97fe-934833790032")]
        public readonly InputSlot<float> Value = new InputSlot<float>();

        [Input(Guid = "da40ddcd-cef8-494a-a7f8-cd8ddfbd7603")]
        public readonly InputSlot<float> Duration = new InputSlot<float>();

        [Input(Guid = "5580eee8-db7b-4e9b-898d-5c7680b0c302")]
        public readonly InputSlot<bool> UseAppRunTime = new InputSlot<bool>();

        [Input(Guid = "fe0231d9-03b6-466f-82e9-852b892adf2e", MappedType = typeof(EaseDirection))]
        public readonly InputSlot<int> Direction = new InputSlot<int>();

        [Input(Guid = "bc388ea3-e1e6-4773-95e5-b8a649c3344f", MappedType = typeof(EasingType))]
        public readonly InputSlot<int> Mode = new InputSlot<int>();
        
    }
}
