using System;
using T3.Core.Animation;
using T3.Core.Logging;

namespace T3.Core.Operator.Slots
{
    public class TimeClipSlot<T> : Slot<T>, ITimeClip
    {
        TimeRange _timeRange = new TimeRange(0.0f, 4.0f);
        TimeRange _sourceRange = new TimeRange(0.0f, 4.0f);
        
        // ITimeClip implementation
        public ref TimeRange TimeRange => ref _timeRange;
        public ref TimeRange SourceRange => ref _sourceRange;
        public int LayerIndex { get; set; } = 0;
        public string Name => "kjfljdl";

        private void UpdateWithTimeRangeCheck(EvaluationContext context)
        {
            if (context.TimeInBars >= TimeRange.Start && context.TimeInBars < TimeRange.End)
            {
                _baseUpdateAction(context);
            } 
        }
        
        private Action<EvaluationContext> _baseUpdateAction;
        public override Action<EvaluationContext> UpdateAction
        {
            set
            {
                _baseUpdateAction = value;
                base.UpdateAction = UpdateWithTimeRangeCheck;
            }
        }
    }
}