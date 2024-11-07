using System;
using System.Numerics;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_93d9b8f4_d92f_4727_8d06_c1add3a74fe7
{
    public class EasingsVec2 : Instance<EasingsVec2>
    {
        [Output(Guid = "8e972216-4a0b-47a3-856a-37a3861e2017", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Vector2> Result = new();

        private const float MinTimeElapsedBeforeEvaluation = 1 / 1000f;

        public EasingsVec2()
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
           
            if (Vector2.Distance(inputValue, _previousInputValue) > 0.001f)
            {
                _startTime = currentTime;
                _initialValue = Result.Value;
                _targetValue = inputValue;
            }


            // Calculate progress based on elapsed time and duration
            var elapsedTime = (float)(currentTime - _startTime);
            var progress = Math.Clamp(elapsedTime / duration, 0f, 1f);

            // Apply selected easing function based on easeMode
            //var easedProgress = progress;
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
            Result.Value = new Vector2(
                            MathUtils.Lerp(_initialValue.X, _targetValue.X, easedProgress),
                            MathUtils.Lerp(_initialValue.Y, _targetValue.Y, easedProgress)
                );
            

            _previousInputValue = inputValue;
        }
     

        

        private double _lastEvalTime;
        private double _startTime;
        private Vector2 _initialValue;
        private Vector2 _targetValue;
        private Vector2 _previousInputValue;

        [Input(Guid = "3156deda-67ba-42eb-9418-7f28ea4b4ca5")]
        public readonly InputSlot<System.Numerics.Vector2> Value = new();

        [Input(Guid = "51989ff4-a9c6-4fbc-bbc1-79b7af859e23")]
        public readonly InputSlot<float> Duration = new();

        [Input(Guid = "38f566a4-d41f-43d7-9ee5-1be3cb20e21c")]
        public readonly InputSlot<bool> UseAppRunTime = new();

        [Input(Guid = "19da524f-6cea-4da0-8ee2-9b47ef8a93a7", MappedType = typeof(EasingFunctions.EasingType))]
        public readonly InputSlot<int> Ease = new();

        


        
    }
}
