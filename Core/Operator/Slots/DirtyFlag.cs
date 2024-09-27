using System;
using System.Runtime.CompilerServices;

namespace T3.Core.Operator.Slots;

public sealed class DirtyFlag
{

    public static void IncrementGlobalTicks()
    {
        _globalTickCount += GlobalTickDiffPerFrame;
    }

    public bool IsDirty => TriggerIsEnabled || Reference != Target;

    public static int InvalidationRefFrame = 0;

    public int Invalidate()
    {
        // Returns the Target - should be no performance hit according to:
        // https://stackoverflow.com/questions/12200662/are-void-methods-at-their-most-basic-faster-less-of-an-overhead-than-methods-tha
            
        // Debug.Assert(InvalidationRefFrame != InvalidatedWithRefFrame); // this should never happen and prevented on the calling side

        if (InvalidationRefFrame != InvalidatedWithRefFrame)
        {
            // the ref frame prevent double invalidation when outputs are connected several times
            InvalidatedWithRefFrame = InvalidationRefFrame;
            Target++;
        }
        //else
        //{
        //    Log.Error("Double invalidation of a slot. Please notify cynic about current setup.");
        //}

        return Target;
    }

    public void ForceInvalidate()
    {
        InvalidatedWithRefFrame = InvalidationRefFrame;
        Target++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        Reference = Target;
    }

    internal void SetUpdated()
    {
        Clear();
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

    public DirtyFlagTrigger Trigger
    {
        get => _trigger;
        set
        {
            _trigger = value;
            TriggerIsEnabled = value != DirtyFlagTrigger.None;
            TriggerIsAnimated = value == DirtyFlagTrigger.Animated;
        }
    }
    private DirtyFlagTrigger _trigger;
    internal bool TriggerIsEnabled;
    internal bool TriggerIsAnimated;

    public int NumUpdatesWithinFrame
    {
        get
        {
            var updatesSinceLastFrame = _lastUpdateTick - _globalTickCount;
            var shift = (-updatesSinceLastFrame >= GlobalTickDiffPerFrame) ? GlobalTickDiffPerFrame : 0;
            return Math.Max(updatesSinceLastFrame + shift + 1, 0); // shift corrects if update was one frame ago
        }
    }

        
    internal int InvalidatedWithRefFrame = -1;
    private const int GlobalTickDiffPerFrame = 100; // each frame differs with 100 ticks to last one
    private static int _globalTickCount;
    private int _lastUpdateTick;
}