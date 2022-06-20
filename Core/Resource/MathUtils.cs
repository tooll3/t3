using System;
using System.Numerics;

namespace T3.Core
{
    public static class MathUtils
    {
        public static float ToRad => (float)(Math.PI / 180.0);
        public static float ToDegree => (float)(180.0 / Math.PI);

        public static float PerlinNoise(float value, float period, int octaves, int seed)
        {
            var noiseSum = 0.0f;

            var frequency = period;
            var amplitude = 0.5f;
            for (var octave = 0; octave < octaves - 1; octave++)
            {
                var v = value * frequency + seed * 12.468f;
                var a = Noise((int)v, seed);
                var b = Noise((int)v + 1, seed);
                var t = Fade(v - (float)Math.Floor(v));
                noiseSum += SharpDX.MathUtil.Lerp(a, b, t) * amplitude;
                frequency *= 2;
                amplitude *= 0.5f;
            }

            return noiseSum;
        }

        private static float Noise(int x, int seed)
        {
            int n = x + seed * 137;
            n = (n << 13) ^ n;
            return (float)(1.0 - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0);
        }

        private static float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        public static float SmootherStep(float min, float max, float value)
        {
            var t = Math.Max(0, Math.Min(1, (value - min) / (max - min)));
            return Fade(t);
        }

        private static float SmoothStep(float min, float max, float value)
        {
            var x = Math.Max(0, Math.Min(1, (value - min) / (max - min)));
            return x * x * (3 - 2 * x);
        }

        private static double SmoothStep(double min, double max, double value)
        {
            var x = Math.Max(0, Math.Min(1, (value - min) / (max - min)));
            return x * x * (3 - 2 * x);
        }

        public static Vector2 Clamp(Vector2 v, Vector2 mn, Vector2 mx)
        {
            return new Vector2((v.X < mn.X)
                                   ? mn.X
                                   : (v.X > mx.X)
                                       ? mx.X
                                       : v.X, (v.Y < mn.Y) ? mn.Y : (v.Y > mx.Y) ? mx.Y : v.Y);
        }

        public static T Min<T>(T lhs, T rhs) where T : System.IComparable<T>
        {
            return lhs.CompareTo(rhs) < 0 ? lhs : rhs;
        }

        public static T Max<T>(T lhs, T rhs) where T : System.IComparable<T>
        {
            return lhs.CompareTo(rhs) >= 0 ? lhs : rhs;
        }

        public static T Clamp<T>(this T val, T min, T max) where T : System.IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        public static float Lerp(float a, float b, float t)
        {
            return (float)(a + (b - a) * t);
        }

        public static float Fmod(float v, float mod)
        {
            return v - mod * (float)Math.Floor(v / mod);
        }

        public static double Fmod(double v, double mod)
        {
            return v - mod * Math.Floor(v / mod);
        }

        public static float RemapAndClamp(float value, float inMin, float inMax, float outMin, float outMax)
        {
            var factor = (value - inMin) / (inMax - inMin);
            var v = factor * (outMax - outMin) + outMin;
            if (outMin > outMax)
                Utilities.Swap(ref outMin, ref outMax);
            return v.Clamp(outMin, outMax);
        }

        public static double Remap(double value, double inMin, double inMax, double outMin, double outMax)
        {
            var factor = (value - inMin) / (inMax - inMin);
            var v = factor * (outMax - outMin) + outMin;
            if (v > outMax)
            {
                v = outMax;
            }
            else if (v < outMin)
            {
                v = outMin;
            }

            return v;
        }

        public static Vector2 Min(Vector2 lhs, Vector2 rhs)
        {
            return new Vector2(lhs.X < rhs.X ? lhs.X : rhs.X, lhs.Y < rhs.Y ? lhs.Y : rhs.Y);
        }

        public static Vector2 Floor(Vector2 v)
        {
            return new Vector2((float)Math.Floor(v.X), (float)Math.Floor(v.Y));
        }

        public static Vector2 Max(Vector2 lhs, Vector2 rhs)
        {
            return new Vector2(lhs.X >= rhs.X ? lhs.X : rhs.X, lhs.Y >= rhs.Y ? lhs.Y : rhs.Y);
        }

        public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            return new Vector2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        }

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            return new Vector3(a.X + (b.X - a.X) * t,
                               a.Y + (b.Y - a.Y) * t,
                               a.Z + (b.Z - a.Z) * t);
        }

        public static Vector4 Lerp(Vector4 a, Vector4 b, float t)
        {
            return new Vector4(a.X + (b.X - a.X) * t,
                               a.Y + (b.Y - a.Y) * t,
                               a.Z + (b.Z - a.Z) * t,
                               a.W + (b.W - a.W) * t);
        }

        public static double Lerp(double a, double b, double t)
        {
            return (double)(a + (b - a) * t);
        }

        public static int Lerp(int a, int b, float t)
        {
            return (int)(a + (b - a) * t);
        }

        public static double Log2(double value)
        {
            return Math.Log10(value) / Math.Log10(2.0);
        }

        public static float RoundValue(float i, float stepsPerUnit, float stepRatio)
        {
            float u = 1 / stepsPerUnit;
            float v = stepRatio / (2 * stepsPerUnit);
            float m = i % u;
            float r = m - (m < v
                               ? 0
                               : (m > (u - v))
                                   ? u
                                   : ((m - v) / (1 - 2 * stepsPerUnit * v)));
            float y = i - r;
            return y;
        }

        /// <summary>
        /// Smooth damps a value with a "critically damped spring" similar to unity's SmoothDamp helper method.
        /// See https://stackoverflow.com/a/5100956 
        /// </summary>
        public static float SpringDamp(float target,
                                                   float current,
                                                   ref float velocity,
                                                   float springConstant = 2,
                                                   float timeStep = 1/60f)
        {
            //const float springConstant = 0.41f;
            var currentToTarget = target - current;
            var springForce = currentToTarget * springConstant;
            var dampingForce = -velocity * 2 * MathF.Sqrt(springConstant);
            var force = springForce + dampingForce;
            velocity += force * timeStep;
            var displacement = velocity * timeStep;
            return current + displacement;
        }

        public const float Pi2 = (float)Math.PI * 2;

        public static Vector3 ToVector3(this Vector4 vec)
        {
            return new Vector3(vec.X / vec.W, vec.Y / vec.W, vec.Z / vec.W);
        }

        public static SharpDX.Vector3 ToVector3(this SharpDX.Vector4 vec)
        {
            return new SharpDX.Vector3(vec.X / vec.W, vec.Y / vec.W, vec.Z / vec.W);
        }

        public static SharpDX.Vector3 ToSharpDx(this Vector3 source)
        {
            return new SharpDX.Vector3(source.X, source.Y, source.Z);
        }

        public static Vector3 ToNumerics(this SharpDX.Vector3 source)
        {
            return new Vector3(source.X, source.Y, source.Z);
        }

        public static SharpDX.Vector4 ToSharpDx(this Vector4 source)
        {
            return new SharpDX.Vector4(source.X, source.Y, source.Z, source.W);
        }

        public static Vector4 ToNumerics(this SharpDX.Vector4 source)
        {
            return new Vector4(source.X, source.Y, source.Z, source.W);
        }

        public static SharpDX.Vector4 ToSharpDxVector4(this Vector3 source, float w)
        {
            return new SharpDX.Vector4(source.X, source.Y, source.Z, w);
        }

        public static SharpDX.Vector3 ToSharpDxVector3(this Vector3 source)
        {
            return new SharpDX.Vector3(source.X, source.Y, source.Z);
        }
    }

    public class EaseFunctions
    {
        public static float EaseOutElastic(float x)
        {
            const float c4 = (float)(2 * Math.PI) / 3;

            return x <= 0f
                       ? 0f
                       : x >= 1f
                           ? 1f
                           : (float)(Math.Pow(2, -10 * x) * Math.Sin((x * 10 - 0.75) * c4) + 1);
        }
    }
}