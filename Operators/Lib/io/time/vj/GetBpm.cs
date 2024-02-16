using System.Runtime.InteropServices;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.io.time.vj
{
	[Guid("6ae8ebb8-3174-463d-9ffb-e14e12eb3029")]
    public class GetBpm : Instance<GetBpm>
    {
        [Output(Guid = "551EBFF2-2044-4F28-A6BA-2384A74C8919")]
        public readonly Slot<float> Result = new();
        
        public GetBpm()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {

            if (Playback.Current == null)
            {
                Log.Warning("Can't get BPM rate without value playback", this);
                return;
            }

            Result.Value = (float)Playback.Current.Bpm;
        }
    }
}