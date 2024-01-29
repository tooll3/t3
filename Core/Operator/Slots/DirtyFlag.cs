using System;

namespace T3.Core.Operator.Slots
{
    public sealed class DirtyFlag
    {
        private const int GLOBAL_TICK_DIFF_PER_FRAME = 100; // each frame differs with 100 ticks to last one
        private static int _globalTickCount = 0;

        public static void IncrementGlobalTicks()
        {
            _globalTickCount += GLOBAL_TICK_DIFF_PER_FRAME;
        }

        public bool IsDirty => Reference != Target || Trigger == DirtyFlagTrigger.Always;

        public static int InvalidationRefFrame { get; set; } = 0;
        private int _invalidatedWithRefFrame = -1;
        private int _visitedAtFrame = -1;
        public bool IsAlreadyInvalidated => InvalidationRefFrame == _invalidatedWithRefFrame;

        public void SetVisited() 
        {
            _visitedAtFrame = InvalidationRefFrame;
        }

        public bool HasBeenVisited => _visitedAtFrame == InvalidationRefFrame;

        public void Invalidate(bool forceInvalidation = false)
        {
            // Debug.Assert(!IsAlreadyInvalidated); // this should never happen and prevented on the calling side
            if (!IsAlreadyInvalidated || forceInvalidation)
            {
                // the ref frame prevent double invalidation when outputs are connected several times
                _invalidatedWithRefFrame = InvalidationRefFrame;
                Target++;
            }
            else
            {
                //Log.Error("Double invalidation of a slot. Please notify cynic about current setup.");
            }
        }

        public void Clear()
        {
            Reference = Target;
        }

        public void SetUpdated()
        {
            if (LastUpdate >= _globalTickCount && LastUpdate < _globalTickCount + GLOBAL_TICK_DIFF_PER_FRAME - 1)
            {
                LastUpdate++;
            }
            else
            {
                LastUpdate = _globalTickCount;
            }
        }

        public int Reference = 0;
        public int Target = 1; // initially dirty
        public int LastUpdate = 0;
        public int FramesSinceLastUpdate => (_globalTickCount - 1 - LastUpdate) / GLOBAL_TICK_DIFF_PER_FRAME;

        public int NumUpdatesWithinFrame
        {
            get
            {
                int diff = LastUpdate - _globalTickCount;
                int shift = (-diff >= GLOBAL_TICK_DIFF_PER_FRAME) ? GLOBAL_TICK_DIFF_PER_FRAME : 0;
                return Math.Max(diff + shift + 1, 0); // shift corrects if update was one frame ago
            }
        }

        public DirtyFlagTrigger Trigger;
    }
}