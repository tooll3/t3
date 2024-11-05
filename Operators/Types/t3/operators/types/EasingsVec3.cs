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
            var strength = Strength.GetValue(context);

            var currentTime = UseAppRunTime.GetValue(context) ? Playback.RunTimeInSecs : context.LocalFxTime;
            if (Math.Abs(currentTime - _lastEvalTime) < MinTimeElapsedBeforeEvaluation)
                return;

            if (context.IntVariables.TryGetValue("__MotionBlurPass", out var motionBlurPass) && motionBlurPass > 0)
            {
                return;
            }

            _lastEvalTime = currentTime;
           /* var threshold = new Vector3(0.001f, 0.001f, 0.001f);
            var testing = _previousInputValue.X;
            var inval = inputValue.X;
            // Check if input has changed to trigger new animation
            if (Math.Abs(inval - testing) > 0.001f)
            {
                _startTime = currentTime;
                _initialValue = Result.Value;
                _targetValue = inputValue;
            }*/
            if (Vector3.Distance(inputValue, _previousInputValue) > 0.001f)
            {
                _startTime = currentTime;
                _initialValue = Result.Value;
                _targetValue = inputValue;
            }


            // Calculate progress based on elapsed time and duration
            float elapsedTime = (float)(currentTime - _startTime);
            float progress = Math.Clamp(elapsedTime / duration, 0f, 1f);

            // Apply selected easing function based on easeMode
            float easedProgress = progress;
            switch (easeMode)
            {
                case 1:
                    easedProgress = InOutCubic(progress);
                    break;
                case 2:
                    easedProgress = InOutExpo(progress);
                    break;
                case 3:
                    easedProgress = InOutBack(progress, strength);
                    break;
                case 4:
                    easedProgress = OutBounce(progress);
                    break;
                case 5:
                    easedProgress = OutElastic(progress);
                    break;
                case 6:
                    easedProgress = OutCirc(progress);
                    break;
                case 7:
                     easedProgress = OutBack(progress, strength);
                    break;
                case 8:
                    easedProgress = OutQuad(progress);
                    break;
                case 9: 
                    easedProgress = OutExpo(progress);
                    break;
                case 10:
                    easedProgress = OutCubic(progress);
                    break;
                case 11:
                    easedProgress = InQuad(progress);
                    break;
                case 12:
                    easedProgress = InCubic(progress);
                    break;

                default:
                    // Default to linear if easeMode is unrecognized or set to 0
                    easedProgress = progress;
                    break;
            }
            Result.Value = new Vector3(
                            MathUtils.Lerp(_initialValue.X, _targetValue.X, easedProgress),
                            MathUtils.Lerp(_initialValue.Y, _targetValue.Y, easedProgress),
                            MathUtils.Lerp(_initialValue.Z, _targetValue.Z, easedProgress)
                );

            _previousInputValue = inputValue;
        }
        private enum Easings
        {
            Linear = 0,
            InOutCubic = 1,
            InOutExpo = 2,
            InOutBack = 3,
            OutBounce = 4,
            OutElastic = 5,
            OutCirc = 6,
            OutBack = 7,
            OutQuad = 8,
            OutExpo = 9,
            OutCubic = 10,
            InQuad = 11,
            InCubic = 12,

        }

        // Easing functions

            //Quad
        private static float InQuad(float t) => t * t;
        private static float OutQuad(float t) => t * (2 - t);
        private static float InOutQuad(float t) => t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
            //Cubic
        private static float InCubic(float t) => t * t * t;
        private static float OutCubic(float t) => (--t) * t * t + 1;
        private static float InOutCubic(float t) => t < 0.5 ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;
            //Expo
        private static float InExpo(float t)
        {
            return (float)(t == 0 ? 0 : Math.Pow(2, 10 * t - 10));
        }
        private static float OutExpo(float t)
        {
            return (float)(t == 1 ? 1 : 1 - Math.Pow(2, -10 * t));
        }
        private static float InOutExpo(float t)
        {
            return (float)(t == 0
              ? 0
              : t == 1
              ? 1
              : t < 0.5 ? Math.Pow(2, 20 * t - 10) / 2
              : (2 - Math.Pow(2, -20 * t + 10)) / 2);
        }
            //Back
        private static float InBack(float t, float ctrl )
        {
            var c1 = 1.70158f + ctrl;
            var c3 = c1 + 1f;

            return (float)c3 * t * t * t - c1 * t * t;

        }
        private static float OutBack(float t, float ctrl)
        {
            var c1 = 1.70158f + ctrl;
            var c3 = c1 + 1;

            return 1 + c3 * MathF.Pow(t - 1, 3) + c1 * MathF.Pow(t - 1, 2);
        }
        private static float InOutBack(float t, float ctrl)
        {
            var c1 = 1.70158f + ctrl;
            var c2 = c1 * 1.525;

            return (float)(t < 0.5
            ? (Math.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2)) / 2
            : (Math.Pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2);

        }
            //Bounce
        private static float OutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1 / d1)
            {
                return n1 * t * t;
            }
            else if (t < 2 / d1)
            {
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            }
            else if (t < 2.5f / d1)
            {
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            }
            else
            {
                return n1 * (t -= 2.625f / d1) * t + 0.984375f;
            }
        }
            //Elastic
        private static float InElastic(float t)
        {
            const float c4 = (float)((2f * Math.PI) / 3f);

            return (float)(t == 0
              ? 0
              : t == 1
              ? 1
              : -Math.Pow(2, 10 * t - 10) * Math.Sin((t * 10 - 10.75) * c4));
        }
        private static float OutElastic(float t)
        {
            const float c4 = (float)((2f * Math.PI) / 3f);

            return (float)(t == 0
              ? 0
              : t == 1
              ? 1
              : Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c4) + 1);
        }
            //Circ
        private static float InCirc(float t)
        {
            return (float)(1 - Math.Sqrt(1 - Math.Pow(t, 2)));
        }
        private static float OutCirc(float t)
        {
            return (float)Math.Sqrt(1 - Math.Pow(t - 1, 2));
        }
        private static float InOutCirc(float t)
        {
            return (float)(t < 0.5
                ? (1 - Math.Sqrt(1 - Math.Pow(2 * t, 2))) / 2
                : (Math.Sqrt(1 - Math.Pow(-2 * t + 2, 2)) + 1) / 2);
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

        [Input(Guid = "9ef45ff9-9d59-44b7-b03d-37a1be40a776", MappedType = typeof(Easings))]
        public readonly InputSlot<int> Ease = new();

        [Input(Guid = "fb3876a7-0a01-4cdb-891b-a3557f46c75a")]
        public readonly InputSlot<float> Strength = new();


        
    }
}
