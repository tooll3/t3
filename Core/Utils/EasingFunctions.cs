using System;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace T3.Core.Utils
{
    public static class EasingFunctions
    {
        // Easing functions
        //Sine
        public static float InSine(float t)
        {
            return 1f - MathF.Cos((t * MathF.PI) / 2f);
        }

        public static float OutSine(float t)
        {
            return MathF.Sin((t * MathF.PI) / 2);
        }

        public static float InOutSine(float t)
        {
            return (-(MathF.Cos(MathF.PI * t) - 1) / 2);
        }

        //Quad
        public static float InQuad(float t)
        {
            return t * t;
        }

        public static float OutQuad(float t)
        {
            return t * (2 - t);
        }

        public static float InOutQuad(float t)
        {
            return t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
        }

        //Cubic
        public static float InCubic(float t)
        {
            return t * t * t;
        }

        public static float OutCubic(float t)
        {
            return (1 - MathF.Pow(1 - t, 3));
        }

        public static float InOutCubic(float t)
        {
            return t < 0.5 ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;
        }

        //Quart
        public static float InQuart(float t)
        {
            return t * t * t * t;
        }

        public static float OutQuart(float t)
        {
            return (1 - MathF.Pow(1 - t, 4));
        }

        public static float InOutQuart(float t)
        {
            return (t < 0.5 ? 8 * t * t * t * t : 1 - MathF.Pow(-2 * t + 2, 4) / 2);
        }

        //Quint
        public static float InQuint(float t)
        {
            return t * t * t * t * t;
        }

        public static float OutQuint(float t)
        {
            return (1 - MathF.Pow(1 - t, 5));
        }

        public static float InOutQuint(float t)
        {
            return (t < 0.5 ? 16 * t * t * t * t * t : 1 - MathF.Pow(-2 * t + 2, 5) / 2);
        }

        //Expo
        public static float InExpo(float t)
        {
            return (t == 0 ? 0 : MathF.Pow(2, 10 * t - 10));
        }
        public static float OutExpo(float t)
        {
            return (t == 1 ? 1 : 1 - MathF.Pow(2, -10 * t));
        }
        public static float InOutExpo(float t)
        {
            return (t == 0
              ? 0
              : t == 1
              ? 1
              : t < 0.5 ? MathF.Pow(2, 20 * t - 10) / 2
              : (2 - MathF.Pow(2, -20 * t + 10)) / 2);
        }
        //Circ
        public static float InCirc(float t)
        {
            return (1 - MathF.Sqrt(1 - MathF.Pow(t, 2)));
        }
        public static float OutCirc(float t)
        {
            return MathF.Sqrt(1 - MathF.Pow(t - 1, 2));
        }
        public static float InOutCirc(float t)
        {
            return (t < 0.5
                ? (1 - MathF.Sqrt(1 - MathF.Pow(2 * t, 2))) / 2
                : (MathF.Sqrt(1 - MathF.Pow(-2 * t + 2, 2)) + 1) / 2);
        }
        //Back
        public static float InBack(float t)
        {
            var c1 = 1.70158f;
            var c3 = c1 + 1f;

            return c3 * t * t * t - c1 * t * t;

        }
        public static float OutBack(float t)
        {
            var c1 = 1.70158f;
            var c3 = c1 + 1;

            return 1 + c3 * MathF.Pow(t - 1, 3) + c1 * MathF.Pow(t - 1, 2);
        }
        public static float InOutBack(float t)
        {
            var c1 = 1.70158f;
            var c2 = c1 * 1.525f;

            return (t < 0.5f
            ? (MathF.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2)) / 2f
            : (MathF.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) / 2f);

        }
        //Elastic
        public static float InElastic(float t)
        {
            const float c4 = ((2f * MathF.PI) / 3f);

            return (t == 0
              ? 0
              : t == 1
              ? 1
              : -MathF.Pow(2, 10 * t - 10) * MathF.Sin((t * 10 - 10.75f) * c4));
        }
        public static float OutElastic(float t)
        {
            const float c4 = ((2f * MathF.PI) / 3f);

            return (t == 0
              ? 0
              : t == 1
              ? 1
              : MathF.Pow(2, -10 * t) * MathF.Sin((t * 10 - 0.75f) * c4) + 1);
        }
        public static float InOutElastic(float t)
        {
            const float c5 = ((2 * MathF.PI) / 4.5f);

            return (t == 0
                ? 0
                : t == 1
                ? 1
                : t < 0.5
                ? -(MathF.Pow(2, 20 * t - 10) * MathF.Sin((20 * t - 11.125f) * c5)) / 2
                : (MathF.Pow(2, -20 * t + 10) * MathF.Sin((20 * t - 11.125f) * c5)) / 2 + 1);


        }
        //Bounce
        public static float InBounce(float t)
        {
            return 1 - OutBounce(1 - t);
        }

        public static float OutBounce(float t)
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
        public static float InOutBounce(float t)
        {
            return t < 0.5 ? (1 - OutBounce(1 - 2 * t)) / 2 : (1 + OutBounce(2 * t - 1)) / 2;
        }

        public enum Interpolations
        {
            Linear = 0,
            Sine = 1,
            Quad = 2,
            Cubic = 3,
            Quart = 4,
            Quint = 5,
            Expo = 6,
            Circ = 7,
            Back = 8,
            Elastic = 9,
            Bounce = 10,
          
        }

        public enum EaseDirection
        {
            In = 0,
            Out = 1,
            InOut = 2,
        }

    }
}