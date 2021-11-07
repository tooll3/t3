using System;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_c5e39c67_256f_4cb9_a635_b62a0d9c796c
{
    public class LFO : Instance<LFO>
    {
        [Output(Guid = "c47e8843-6e8d-4eaf-a554-874b3af9ee63", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Result = new Slot<float>();

        public LFO()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            _phase = Phase.GetValue(context);
            _bias = Bias.GetValue(context);
            _shape = (Shapes)Shape.GetValue(context);
            _ratio = Ratio.GetValue(context);
            var f = (SpeedFactors)AllowSpeedFactor.GetValue(context);
            switch (f)
            {
                case SpeedFactors.None:
                    _speedFactor = 1;
                    break;
                case SpeedFactors.FactorA:
                {
                    if (!context.FloatVariables.TryGetValue(SpeedFactorA, out _speedFactor))
                        _speedFactor = 1;
                    
                    break;
                }
                case SpeedFactors.FactorB:
                    if (!context.FloatVariables.TryGetValue(SpeedFactorB, out _speedFactor))
                        _speedFactor = 1;

                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var time = OverrideTime.IsConnected
                           ? OverrideTime.GetValue(context)
                           : context.TimeForEffects;
            
            var rate = Rate.GetValue(context);

            var t = time * rate * _speedFactor;
            LastFraction =(float)MathUtils.Fmod(t,1);
            
            var normalizedValue = CalcNormalizedValueForFraction(t);
            Result.Value = normalizedValue * Amplitude.GetValue(context) + Offset.GetValue(context);
        }

        public float CalcNormalizedValueForFraction(double t)
        {
            var fraction = CalcFraction(t);
            var value = MapShapes[(int)_shape](fraction);
            var biased = SchlickBias(value, _bias);
            return biased;
        }

        private float CalcFraction(double t)
        {
            return ((float)MathUtils.Fmod(t + _phase, 1) / _ratio).Clamp(0,1);
        } 
        
        
        private float SchlickBias(float x, float bias)
        {
            return x / ((1 / bias - 2) * (1 - x) + 1);
        }


        private delegate float MappingFunction(float fraction);
        private static readonly MappingFunction[] MapShapes =
            {
                f => f, // 0:Ramp
                f => 1 - f, // 1:Saw,
                f => (float)Math.Sin((f + 0.25) * 2 * 3.141592f)/ 2 +0.5f, // 2:Wave
                f => f > 0.5f ? 1 : 0, // 3: Square
                f => f < 0.5f ? (f * 2) : (1 - (f - 0.5f) * 2), //4: ZigZag
            };

        private Shapes _shape;
        private float _bias;
        private float _phase;
        private float _ratio = 1;
        private float _speedFactor = 1;

        
        public float LastFraction;

        public enum Shapes
        {
            Ramp = 0,
            Saw = 1,
            Wave = 2,
            Square = 3,
            ZigZag = 4,
        }

        [Input(Guid = "4C38C34C-D992-47F1-BCB5-9BD13FC6474B", MappedType = typeof(Shapes))]
        public readonly InputSlot<int> Shape = new InputSlot<int>();

        [Input(Guid = "a4d48d80-936c-4bbb-a2e8-32f86edd4ab2")]
        public readonly InputSlot<float> Rate = new InputSlot<float>();

        [Input(Guid = "3b9e7272-ccf3-4fff-a079-5fcbb8a6c7d5")]
        public readonly InputSlot<float> Ratio = new InputSlot<float>();

        [Input(Guid = "36ae5b4b-62e9-49c0-b841-97394122cb1e")]
        public readonly InputSlot<float> Phase = new InputSlot<float>();

        [Input(Guid = "8a5033c2-7d22-44d7-9472-d23677b11388")]
        public readonly InputSlot<float> Amplitude = new InputSlot<float>();

        [Input(Guid = "126511E6-771D-4DD0-8A9D-1861C7B45D23")]
        public readonly InputSlot<float> Offset = new InputSlot<float>();

        [Input(Guid = "3396DE1F-03AF-43EE-A43A-55016BEC70AE")]
        public readonly InputSlot<float> Bias = new InputSlot<float>();

        [Input(Guid = "76ca8a8b-f252-4687-805e-fb7a86a16567")]
        public readonly InputSlot<float> OverrideTime = new InputSlot<float>();
        
        [Input(Guid = "6ca8a8b2-f252-4687-805e-fb7a86a16567", MappedType = typeof(SpeedFactors))]
        public readonly InputSlot<int> AllowSpeedFactor = new InputSlot<int>();

        public enum SpeedFactors {
            None,
            FactorA,
            FactorB,
        }
        
        public const string SpeedFactorA = "SpeedFactorA";
        public const string SpeedFactorB = "SpeedFactorB";
    }
}