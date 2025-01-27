using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_6ae8ebb8_3174_463d_9ffb_e14e12eb3029
{
    public class GetBpm : Instance<GetBpm>
    {
        [Output(Guid = "551EBFF2-2044-4F28-A6BA-2384A74C8919", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Result = new();
        
        public GetBpm()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (Playback.Current == null)
            {
              
                return;
            }

            Result.Value = (float)Playback.Current.Bpm;

          
        }

    }
}