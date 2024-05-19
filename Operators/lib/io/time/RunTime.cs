using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_862de1a8_f630_4823_8860_7afa918bb1bc
{
    public class RunTime : Instance<RunTime>
    {
        [Output(Guid = "{1C34D39C-0BEF-4C4A-A3E4-DCB8D5664F3B}", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> TimeInSeconds = new();

        public RunTime()
        {
            TimeInSeconds.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            TimeInSeconds.Value = (float)Playback.RunTimeInSecs;
        }
    }
}