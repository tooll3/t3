using System.Diagnostics;

namespace T3.Core.Operator.Types
{
    public class Time : Instance<Time>
    {
        [Output]
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