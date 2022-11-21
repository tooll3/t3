using System;
using System.Numerics;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_af79ee8c_d08d_4dca_b478_b4542ed69ad8
{
    public class AnimVec2 : Instance<AnimVec2>
    {
        [Output(Guid = "7757A3F5-EA71-488E-9CEC-0151FFD332CC", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Vector2> Result = new();

        public AnimVec2()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var phases = Phases.GetValue(context);
            var masterRate = RateFactor.GetValue(context);
            var rates = Rates.GetValue(context);
            var rateFactorFromContext = AnimMath.GetSpeedOverrideFromContext(context, AllowSpeedFactor);
            var shape = (AnimMath.Shapes)Shape.GetValue(context).Clamp(0, Enum.GetNames(typeof(AnimMath.Shapes)).Length);
            var amplitudes = Amplitudes.GetValue(context);
            var offsets = Amplitudes.GetValue(context);

            var time = OverrideTime.IsConnected
                           ? OverrideTime.GetValue(context)
                           : context.LocalFxTime;

            // Don't use vector to keep double precision
            var timeX = (time + phases.X) * masterRate * rateFactorFromContext * rates.X;
            var timeY = (time + phases.Y) * masterRate * rateFactorFromContext * rates.Y;

            Result.Value = new Vector2(AnimMath.CalcValueForNormalizedTime(shape, timeX, 0) * amplitudes.X + offsets.X,
                                       AnimMath.CalcValueForNormalizedTime(shape, timeY, 1) * amplitudes.Y + offsets.Y);
        }

        [Input(Guid = "6ebc3788-d3d5-44df-96d7-b88689f9e166", MappedType = typeof(AnimMath.Shapes))]
        public readonly InputSlot<int> Shape = new();

        [Input(Guid = "97530728-a2a8-4d29-8ea4-e2170be70f18")]
        public readonly InputSlot<float> RateFactor = new();

        [Input(Guid = "8923F351-7F6B-46F1-8DF6-9559534278BE")]
        public readonly InputSlot<Vector2> Rates = new();

        [Input(Guid = "140CBA08-E712-4C2B-A625-F270F1B72B54")]
        public readonly InputSlot<Vector2> Amplitudes = new();

        [Input(Guid = "304124E6-1FA1-4F6B-86DE-EF7769CDE1F6")]
        public readonly InputSlot<Vector2> Offsets = new();
        
        [Input(Guid = "62165CC4-9DA8-47DC-89AE-8B6CDE8DDA49")]
        public readonly InputSlot<Vector2> Phases = new();

        [Input(Guid = "603b30b2-6f12-42de-84b6-c772962e9d26")]
        public readonly InputSlot<float> OverrideTime = new();

        [Input(Guid = "7a1f6dc7-2ae8-4cbb-9750-c17e460327d4", MappedType = typeof(AnimMath.Shapes))]
        public readonly InputSlot<int> AllowSpeedFactor = new();
    }
}