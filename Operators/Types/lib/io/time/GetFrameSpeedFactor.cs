using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3be62864_30dd_4531_9980_28b634296e47
{
    public class GetFrameSpeedFactor : Instance<GetFrameSpeedFactor>
    {
        [Output(Guid = "d5de5667-82f7-4556-9b44-5e6ebdcfdd4d", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> FrameSpeedFactor = new();
        
        public GetFrameSpeedFactor()
        {
            FrameSpeedFactor.UpdateAction = Update;
            
        }

        private void Update(EvaluationContext context)
        {
            var isValid = Math.Abs(context.Playback.FrameSpeedFactor) > 0.0001;
            FrameSpeedFactor.Value= isValid ? (float)context.Playback.FrameSpeedFactor:
                                        1;
        }
    }
}