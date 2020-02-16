using System;
using T3.Core.Animation;
using T3.Core.Logging;

namespace T3.Core.Operator.Slots
{
    public class TimeClipSlot<T> : Slot<T>, ITimeClip
    {
        public TimeRange TimeRange { get; set; } = new TimeRange(0.0f, 1.0f);
        public TimeRange SourceRange { get; set; } = new TimeRange(0.0f, 1.0f);

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