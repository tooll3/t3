using System;
using System.Numerics;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_12dccab1_1d7d_4005_a4c1_0bebeeaeb6d3
{
    public class EasingsVec3 : Instance<EasingsVec3>
    {
        [Output(Guid = "b54d5398-3671-44b7-961f-0fb092a2c78b", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Vector3> Result = new();

        private const float MinTimeElapsedBeforeEvaluation = 1 / 1000f;

        public EasingsVec3()
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
           
            if (Vector3.Distance(inputValue, _previousInputValue) > 0.001f)
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
            Result.Value = new Vector3(
                            MathUtils.Lerp(_initialValue.X, _targetValue.X, easedProgress),
                            MathUtils.Lerp(_initialValue.Y, _targetValue.Y, easedProgress),
                            MathUtils.Lerp(_initialValue.Z, _targetValue.Z, easedProgress)
                );
            

            _previousInputValue = inputValue;
        }
     

        

        private double _lastEvalTime;
        private double _startTime;
        private Vector3 _initialValue;
        private Vector3 _targetValue;
        private Vector3 _previousInputValue;

        [Input(Guid = "16e48eb8-3baf-4e0b-a75e-72a747c9fead")]
        public readonly InputSlot<System.Numerics.Vector3> Value = new();

        [Input(Guid = "c7ebf4ed-5477-4461-ac68-8a7ac9849ce6")]
        public readonly InputSlot<float> Duration = new();

        [Input(Guid = "2a450207-489e-4a8d-83ab-37ed8167eebe")]
        public readonly InputSlot<bool> UseAppRunTime = new();

        [Input(Guid = "9ef45ff9-9d59-44b7-b03d-37a1be40a776", MappedType = typeof(EasingFunctions.EasingType))]
        public readonly InputSlot<int> Ease = new();

        


        
    }
}
