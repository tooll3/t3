using System;

namespace T3.Core
{
    public static class MathUtils
    {
        private static float ComputePerlinNoise(float value, float period, int octaves, int seed)
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
            var t = Math.Max(0, Math.Min(1, (value-min)/(max-min)));
            return Fade(t);
        }
        
        private static float SmoothStep(float min, float max, float value)
        {
            var x = Math.Max(0, Math.Min(1, (value-min)/(max-min)));
            return x * x * (3 - 2 * x);
        }
        
        private static double SmoothStep(double min, double max, double value)
        {
            var x = Math.Max(0, Math.Min(1, (value-min)/(max-min)));
            return x * x * (3 - 2 * x);
        }
        
        
    }
}