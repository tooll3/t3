using System;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace T3.Core.Utils
{
    public static class AnimMath
    {
        public static float GetSpeedOverrideFromContext(EvaluationContext context, InputSlot<int> allowSpeedFactor)
        {
            var f = (SpeedFactors)allowSpeedFactor.GetValue(context).Clamp(0, Enum.GetNames(typeof(SpeedFactors)).Length);
            float rateFactorFromContext = 1;
            
            switch (f)
            {
                case SpeedFactors.None:
                    break;
                
                case SpeedFactors.FactorA:
                {
                    if (!context.FloatVariables.TryGetValue(SpeedFactorA, out rateFactorFromContext))
                        rateFactorFromContext = 1;
                    break;
                }
                case SpeedFactors.FactorB:
                    if (!context.FloatVariables.TryGetValue(SpeedFactorB, out rateFactorFromContext))
                        rateFactorFromContext = 1;
                    
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return rateFactorFromContext;
        }
        
        public static float CalcValueForNormalizedTime(Shapes shape, double time, int componentIndex)
        {
            float result = 0;
            switch (shape)
            {
                case Shapes.Ramps:
                case Shapes.Saws:
                case Shapes.Wave:
                case Shapes.Square:
                case Shapes.ZigZag:
                case Shapes.KickSaws:
                case Shapes.Sin:
                    result = _mapShapes[(int)shape](((float)MathUtils.Fmod(time, 1)).Clamp(0, 1));
                    break;

                case Shapes.Random:
                    result = (float)((double)MathUtils.XxHash((uint)(time + 28657 * componentIndex)) / uint.MaxValue);
                    break;

                case Shapes.Endless:
                    result = (float)time;
                    break;

                case Shapes.PerlinNoise:
                    result = MathUtils.PerlinNoise((float)time, 1, 5, 43 * componentIndex);
                    break;
            }
            return result;
        }        
        
        private delegate float MappingFunction(float fraction);
        private static readonly MappingFunction[] _mapShapes =
            {
                f => f, // 0: Endless
                f => f, // 1: Ramps
                f => 1 - MathUtils.Fmod(f, 1), // 2: Saw,
                f => f <= 0 ? 0 : (1 - f.Clamp(0, 1)), // 3: KickSaw (Saw resting at last value)
                f => f > 0.5f ? 1 : 0, // 4: Square
                f =>
                {
                    var ff = MathUtils.Fmod(f, 1);
                    return ff < 0.5f ? (ff * 2) : (1 - (ff - 0.5f) * 2);
                }, // 5: ZigZag,
                f => (float)Math.Sin((f + 0.25) * 2 * 3.141592f) / 2 + 0.5f, // 6: Wave                
                f => (float)Math.Sin((f + 0.25) * 2 * 3.141592f), // 7: Sin
                f => f, // 8: PerlinNoise
                f => f, // 9: Random
            };
        
        public enum Shapes
        {
            Endless = 0,
            Ramps = 1,
            Saws = 2,
            KickSaws = 3,
            Square = 4,
            ZigZag = 5,
            Wave = 6,
            Sin = 7,
            PerlinNoise = 8,
            Random = 9,
        }

        private const string SpeedFactorA = "SpeedFactorA";
        private const string SpeedFactorB = "SpeedFactorB";
        public enum SpeedFactors
        {
            None,
            FactorA,
            FactorB,
        }
    }
}