using System.Collections.Generic;
using System.Linq;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_cda108a1_db4f_4a0a_ae4d_d50e9aade467
{
    public class PlaybackFFT : Instance<PlaybackFFT>
    {
        [Output(Guid = "2d0f5713-9620-4bc7-a792-a7b8e622554a", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<List<float>> Result = new Slot<List<float>>(new List<float>(256));

        public PlaybackFFT()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (!IsEnabled.GetValue(context))
                return;
            
            Result.Value = StreamPlayback.FftBuffer.ToList();
        }
        
        [Input(Guid = "6888315D-EF77-4814-87E9-91528D600C72")]
        public readonly MultiInputSlot<bool> IsEnabled = new MultiInputSlot<bool>();
    }
}