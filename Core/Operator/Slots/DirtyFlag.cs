using System;
using System.Runtime.CompilerServices;

namespace T3.Core.Operator.Slots
{
    public sealed class DirtyFlag
    {

        public static void IncrementGlobalTicks()
        {
            _globalTickCount += GlobalTickDiffPerFrame;
        }

        public bool IsDirty => Reference != Target || Trigger == DirtyFlagTrigger.Always;

        public static int InvalidationRefFrame = 0;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Reference = Target;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetUpdated()
        {
            if (_lastUpdateTick >= _globalTickCount && _lastUpdateTick < _globalTickCount + GlobalTickDiffPerFrame - 1)
            {
                _lastUpdateTick++;
            }
            else
            {
                _lastUpdateTick = _globalTickCount;
            }
        }

        public int Reference;
        public int Target = 1; // initially dirty
        public int FramesSinceLastUpdate => (_globalTickCount - 1 - _lastUpdateTick) / GlobalTickDiffPerFrame;
        public DirtyFlagTrigger Trigger;

        public int NumUpdatesWithinFrame
        {
            get
            {
                var updatesSinceLastFrame = _lastUpdateTick - _globalTickCount;
                var shift = (-updatesSinceLastFrame >= GlobalTickDiffPerFrame) ? GlobalTickDiffPerFrame : 0;
                return Math.Max(updatesSinceLastFrame + shift + 1, 0); // shift corrects if update was one frame ago
            }
        }

        
        private int _invalidatedWithRefFrame = -1;
        private int _visitedAtFrame = -1;
        private const int GlobalTickDiffPerFrame = 100; // each frame differs with 100 ticks to last one
        private static int _globalTickCount;
        private int _lastUpdateTick;
    }
}