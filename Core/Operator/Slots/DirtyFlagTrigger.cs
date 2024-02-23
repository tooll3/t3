using System;

namespace T3.Core.Operator.Slots
{
    public enum DirtyFlagTrigger : byte
    {
        None = 0,
        Always = 0x1,
        Animated = 0x2,
    }
}