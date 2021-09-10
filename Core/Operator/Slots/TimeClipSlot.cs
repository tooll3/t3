using System;
using T3.Core.Animation;
using T3.Core.Logging;

namespace T3.Core.Operator.Slots
{
    public interface ITimeClipProvider
    {
        TimeClip TimeClip { get; }
    }

    public interface IOutputDataUser
    {
        void SetOutputData(IOutputData data);
    }

    // This interface is mainly to extract the output data type while no instance of an implementer exists.
    internal interface IOutputDataUser<T> : IOutputDataUser
    {
    }

    public class TimeClipSlot<T> : Slot<T>, ITimeClipProvider, IOutputDataUser<TimeClip>
    {
        public TimeClip TimeClip { get; private set; }

        public void SetOutputData(IOutputData data)
        {
            TimeClip = data as TimeClip;
            TimeClip.Id = Parent.SymbolChildId;
        }

        private void UpdateWithTimeRangeCheck(EvaluationContext context)
        {
            if ((context.TimeForKeyframes < TimeClip.TimeRange.Start) || (context.TimeForKeyframes >= TimeClip.TimeRange.End))
                return;

            // TODO: Setting local time should flag time accessors as dirty 
            var prevTime = context.TimeForKeyframes;
            double factor = (context.TimeForKeyframes - TimeClip.TimeRange.Start) / (TimeClip.TimeRange.End - TimeClip.TimeRange.Start);
            context.TimeForKeyframes = factor * (TimeClip.SourceRange.End - TimeClip.SourceRange.Start) + TimeClip.SourceRange.Start;
            
            _baseUpdateAction(context);

            context.TimeForKeyframes = prevTime;
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

        protected override void SetDisabled(bool isDisabled)
        {
            if (isDisabled == _isDisabled)
                return;

            if (isDisabled)
            {
                _defaultUpdateAction = _baseUpdateAction;
                base.UpdateAction = EmptyAction;
                DirtyFlag.Invalidate();
            }
            else
            {
                SetUpdateActionBackToDefault();
                DirtyFlag.Invalidate();
            }

            _isDisabled = isDisabled;
        }
    }
}