using System.Diagnostics;
using T3.Core.Operator;

namespace T3.Operators.Types
{
    public class Time : Instance<Time>
    {
        [Output(Guid = "{1C34D39C-0BEF-4C4A-A3E4-DCB8D5664F3B}")]
        public readonly Slot<float> TimeInSeconds = new Slot<float>();

        public Time()
        {
            TimeInSeconds.UpdateAction = Update;
            _watch.Start();
        }

        private void Update(EvaluationContext context)
        {
            TimeInSeconds.Value = _watch.ElapsedMilliseconds / 1000.0f;
        }

        private Stopwatch _watch = new Stopwatch();
    }
}