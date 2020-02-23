using System;
using T3.Core.Animation;
using T3.Core.Logging;

namespace T3.Core.Operator.Slots
{
    public interface ITimeClipProvider
    {
        TimeClip TimeClip { get; }
    }

    public interface IOutputDataConsumer
    {
        void SetOutputData(IOutputData data);
    }

    // This interface is mainly to extract the output data type while no instance of an implementer exists.
    internal interface IOutputDataConsumer<T> : IOutputDataConsumer
    {
    }

    public class TimeClipSlot<T> : Slot<T>, ITimeClipProvider, IOutputDataConsumer<TimeClip>
    {
        public TimeClip TimeClip { get; private set; }

        public void SetOutputData(IOutputData data)
        {
            TimeClip = data as TimeClip;
            TimeClip.Id = Parent.SymbolChildId;
        }

        private void UpdateWithTimeRangeCheck(EvaluationContext context)
        {
            if (context.TimeInBars >= TimeClip.TimeRange.Start && context.TimeInBars < TimeClip.TimeRange.End)
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