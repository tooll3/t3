using System;

namespace T3.Core.Operator.Slots
{
    public class DirtyFlag
    {
        private const int GLOBAL_TICK_DIFF_PER_FRAME = 100; // each frame differs with 100 ticks to last one
        private static int _globalTickCount = 0;

        public static void IncrementGlobalTicks()
        {
            _globalTickCount += GLOBAL_TICK_DIFF_PER_FRAME;
        }

        public bool IsDirty => Reference != Target;

        public static int InvalidationRefFrame { get; set; } = 0;
        private int _invalidatedWithRefFrame = -1;
        public void Invalidate()
        {
            if (InvalidationRefFrame != _invalidatedWithRefFrame)
            {
                // the ref frame prevent double invalidation when outputs are connected several times
                _invalidatedWithRefFrame = InvalidationRefFrame;
                Target++;
            }
        }

        public void Clear()
        {
            Reference = Trigger == DirtyFlagTrigger.Always ? Target - 1 : Target;
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
        public int Target = 1;
        public int LastUpdate = 0;
        public int FramesSinceLastUpdate => (_globalTickCount - 1 - LastUpdate) / GLOBAL_TICK_DIFF_PER_FRAME;
        public int NumUpdatesWithinFrame => Math.Max(LastUpdate - _globalTickCount + GLOBAL_TICK_DIFF_PER_FRAME + 1, 0);
        public DirtyFlagTrigger Trigger;
    }
}