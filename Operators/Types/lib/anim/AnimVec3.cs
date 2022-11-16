using System;
using System.Numerics;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_7814fd81_b8d0_4edf_b828_5165f5657344
{
    public class AnimVec3 : Instance<AnimVec3>
    {
        [Output(Guid = "A77BAA35-3CD9-44D3-9F75-2A4F95FBD595")]
        public readonly Slot<Vector3> Result = new();

        public AnimVec3()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var phases = Offsets.GetValue(context);
            var masterRate = RateFactor.GetValue(context);
            var rates = Rates.GetValue(context);
            var rateFactorFromContext = 1f;
            var shape = (Shapes)Shape.GetValue(context).Clamp(0, Enum.GetNames(typeof(Shapes)).Length);
            var amplitudes = Amplitudes.GetValue(context);
            var offsets = Amplitudes.GetValue(context);
            
            var f = (SpeedFactors)AllowSpeedFactor.GetValue(context).Clamp(0, Enum.GetNames(typeof(SpeedFactors)).Length);
            switch (f)
            {
                case SpeedFactors.None:
                    rateFactorFromContext = 1;
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

            var time = OverrideTime.IsConnected
                           ? OverrideTime.GetValue(context)
                           : context.LocalFxTime;

            var times = new Vector3(  (float)((time + phases.X) * masterRate * rateFactorFromContext * rates.X),
                                      (float)((time + phases.Y) * masterRate * rateFactorFromContext * rates.Y),
                                      (float)((time + phases.Z) * masterRate * rateFactorFromContext * rates.Z));

            var scaledTimes = CalcScaledTimes(shape, times);
            Result.Value = new Vector3(scaledTimes.X * amplitudes.X + offsets.X,
                                       scaledTimes.Y * amplitudes.Y + offsets.Y,
                                       scaledTimes.Z * amplitudes.Z + offsets.Z);
        }

        private Vector3 CalcScaledTimes(Shapes shape, Vector3 times)
        {
            var value = shape == Shapes.Random
                            ? new Vector3((float)MathUtils.XxHash((uint)times.X) / uint.MaxValue,
                                          (float)MathUtils.XxHash((uint)times.Y) / uint.MaxValue,
                                          (float)MathUtils.XxHash((uint)times.Z) / uint.MaxValue)
                            : new Vector3(MapShapes[(int)shape](times.X),
                                          MapShapes[(int)shape](times.Y),
                                          MapShapes[(int)shape](times.Z)
                                         );
            return value;
        }
        
        private delegate float MappingFunction(float fraction);
        private readonly MappingFunction[] MapShapes =
            {
                f => f,                                     // 0 Linear
                f => 1 - MathUtils.Fmod(f,1),         // 1 Saw,
                f =>
                {
                    var ff = MathUtils.Fmod(f, 1);
                    return ff < 0.5f ? (ff * 2) : (1 - (ff - 0.5f) * 2);
                }, // 2 ZigZag,
                f => (float)Math.Sin((f + 0.25) * 2 * 3.141592f), // 3 Sine
                f => MathUtils.Fmod(f,1) > 0.5f ? 1 : 0, //  4 Square
                f => (float)MathUtils.PerlinNoise((float)(f + 0.25) * 2 * 3.141592f, 1,4,43), // Noise
            };
        

        public enum Shapes
        {
            Linear = 0,
            Saw = 1,
            ZigZag = 2,
            Sin = 3,
            Square = 4,
            Noise = 5,
            Random = 6,
        }

        private enum SpeedFactors
        {
            None,
            FactorA,
            FactorB,
        }

        private const string SpeedFactorA = "SpeedFactorA";
        private const string SpeedFactorB = "SpeedFactorB";

        [Input(Guid = "c8faaeca-c153-4d7c-a66b-6916dc7750e3", MappedType = typeof(Shapes))]
        public readonly InputSlot<int> Shape = new();

        [Input(Guid = "754456d0-1ac8-4e31-8d0b-bf1b45db48de")]
        public readonly InputSlot<float> RateFactor = new();

        [Input(Guid = "9F71C196-4C4C-4083-8C27-3047C059F998")]
        public readonly InputSlot<Vector3> Rates = new();

        [Input(Guid = "AAD62703-647D-437B-879B-08793AC8802F")]
        public readonly InputSlot<Vector3> Amplitudes = new();

        [Input(Guid = "A04BB5A4-A7AF-493B-8509-4E9C8B6A94A9")]
        public readonly InputSlot<Vector3> Offsets = new();

        [Input(Guid = "fa1b36a1-4ed9-4187-aeb6-f7ba893cf3b2")]
        public readonly InputSlot<float> OverrideTime = new();

        [Input(Guid = "2d400a08-8926-46bd-b9ba-75ec69fec9dd", MappedType = typeof(SpeedFactors))]
        public readonly InputSlot<int> AllowSpeedFactor = new();
    }
}