using System;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace T3.Core.Utils
{
    public static class EasingFunctions
    {
        // Easing functions
        //Sine
        public static float InSine(float t) => (float)(1-Math.Cos((t * Math.PI)/2));
        public static float OutSine(float t) => (float)Math.Sin((t * Math.PI)/2);
        public static float InOutSine(float t) => (float)(-(Math.Cos(Math.PI * t) - 1) / 2);
        //Quad
        public static float InQuad(float t) => t * t;
        public static float OutQuad(float t) => t * (2 - t);
        public static float InOutQuad(float t) => t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
        //Cubic
        public static float InCubic(float t) => t * t * t;
        public static float OutCubic(float t) => (float)(1 - Math.Pow(1 - t, 3));
        public static float InOutCubic(float t) => t < 0.5 ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;
        //Quart
        public static float InQuart(float t) => t * t * t * t;
        public static float OutQuart(float t) => (float)(1 - Math.Pow(1 - t, 4));
        public static float InOutQuart(float t) =>(float)(t < 0.5 ? 8 * t * t * t * t : 1 - Math.Pow(-2 * t + 2, 4)/2);
        //Quint
        public static float InQuint(float t) => t * t * t * t * t;
        public static float OutQuint(float t) => (float)(1 - Math.Pow(1 - t, 5));
        public static float InOutQuint(float t) => (float)(t < 0.5 ? 16 * t * t * t * t * t : 1 - Math.Pow(-2 * t + 2, 5) / 2);
        //Expo
        public static float InExpo(float t)
        {
            return (float)(t == 0 ? 0 : Math.Pow(2, 10 * t - 10));
        }
        public static float OutExpo(float t)
        {
            return (float)(t == 1 ? 1 : 1 - Math.Pow(2, -10 * t));
        }
        public static float InOutExpo(float t)
        {
            return (float)(t == 0
              ? 0
              : t == 1
              ? 1
              : t < 0.5 ? Math.Pow(2, 20 * t - 10) / 2
              : (2 - Math.Pow(2, -20 * t + 10)) / 2);
        }
        //Circ
        public static float InCirc(float t)
        {
            return (float)(1 - Math.Sqrt(1 - Math.Pow(t, 2)));
        }
        public static float OutCirc(float t)
        {
            return (float)Math.Sqrt(1 - Math.Pow(t - 1, 2));
        }
        public static float InOutCirc(float t)
        {
            return (float)(t < 0.5
                ? (1 - Math.Sqrt(1 - Math.Pow(2 * t, 2))) / 2
                : (Math.Sqrt(1 - Math.Pow(-2 * t + 2, 2)) + 1) / 2);
        }
        //Back
        public static float InBack(float t)
        {
            var c1 = 1.70158f;
            var c3 = c1 + 1f;

            return (float)c3 * t * t * t - c1 * t * t;

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
            var c2 = c1 * 1.525;

            return (float)(t < 0.5
            ? (Math.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2)) / 2
            : (Math.Pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2);

        }
        //Elastic
        public static float InElastic(float t)
        {
            const float c4 = (float)((2f * Math.PI) / 3f);

            return (float)(t == 0
              ? 0
              : t == 1
              ? 1
              : -Math.Pow(2, 10 * t - 10) * Math.Sin((t * 10 - 10.75) * c4));
        }
        public static float OutElastic(float t)
        {
            const float c4 = (float)((2f * Math.PI) / 3f);

            return (float)(t == 0
              ? 0
              : t == 1
              ? 1
              : Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c4) + 1);
        }
        public static float InOutElastic(float t)
        {
            const float c5 = (float)((2 * Math.PI) / 4.5);

            return (float)(t == 0
                ? 0
                : t == 1
                ? 1
                : t < 0.5
                ? -(Math.Pow(2, 20 * t - 10) * Math.Sin((20 * t - 11.125) * c5)) / 2
                : (Math.Pow(2, -20 * t + 10) * Math.Sin((20 * t - 11.125) * c5)) / 2 + 1);


        }
        //Bounce
        public static float InBounce(float t) => 1 - OutBounce(1-t);
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
        public static float InOutBounce(float t) => t < 0.5 ? (1 - OutBounce(1 - 2 * t)) / 2 : (1 + OutBounce(2 * t - 1)) / 2;

        public enum EasingType
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