using System;
using System.Numerics;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
using static T3.Core.Utils.EasingFunctions;

namespace T3.Operators.Types.Id_93d9b8f4_d92f_4727_8d06_c1add3a74fe7
{
    public class EaseVec2 : Instance<EaseVec2>
    {
        [Output(Guid = "8e972216-4a0b-47a3-856a-37a3861e2017", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Vector2> Result = new();

        private const float MinTimeElapsedBeforeEvaluation = 1 / 1000f;

        public EaseVec2()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var inputValue = Value.GetValue(context);  // Target value (0 or 1)
            var duration = Duration.GetValue(context); // Duration of the animation in seconds
            var easeMode = Interpolation.GetValue(context);     // Easing function selector
            var easeDirection = (EaseDirection)Mode.GetValue(context);

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
        public readonly InputSlot<System.Numerics.Vector2> Value = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "51989ff4-a9c6-4fbc-bbc1-79b7af859e23")]
        public readonly InputSlot<float> Duration = new InputSlot<float>();

        [Input(Guid = "38f566a4-d41f-43d7-9ee5-1be3cb20e21c")]
        public readonly InputSlot<bool> UseAppRunTime = new InputSlot<bool>();

        [Input(Guid = "5f2c66dd-dfaf-4aa0-b184-01e89819b317", MappedType = typeof(EaseDirection))]
        public readonly InputSlot<int> Mode = new InputSlot<int>();

        [Input(Guid = "19da524f-6cea-4da0-8ee2-9b47ef8a93a7", MappedType = typeof(EasingFunctions.EasingType))]
        public readonly InputSlot<int> Interpolation = new InputSlot<int>();

        


        
    }
}
