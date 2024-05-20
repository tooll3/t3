using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_5af2405c_35f4_46bf_90db_bb99b0c4a43e
{
    public class LastFrameDuration : Instance<LastFrameDuration>
    {
        [Output(Guid = "04c5cc91-5cfd-4ef5-9dd9-42cb048ce9b5", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Duration = new();
        
        
        public LastFrameDuration()
        {
            Duration.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Duration.Value = (float)Playback.LastFrameDuration;
        }
    }
}