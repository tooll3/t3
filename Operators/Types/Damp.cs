using System;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_af9c5db8_7144_4164_b605_b287aaf71bf6
{
    public class Damp : Instance<Damp>
    {
        [Output(Guid = "aacea92a-c166-46dc-b775-d28baf9820f5", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Result = new Slot<float>();

        public Damp()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var v = Value.GetValue(context);
            var damping = Damping.GetValue(context);

            var f = (float)(damping * EvaluationContext.LastFrameDuration).Clamp(0f,1f);
            _dampedValue = MathUtils.Lerp(v,_dampedValue, f);
            Result.Value = _dampedValue;
        }

        private float _dampedValue;
        
        [Input(Guid = "795aca79-dd10-4f28-a290-a30e7b27b436")]
        public readonly InputSlot<float> Value = new InputSlot<float>();

        [Input(Guid = "F29D5426-5E31-4C7C-BE77-5E45BFB9DAA9")]
        public readonly InputSlot<float> Damping = new InputSlot<float>();
        
    }
}
