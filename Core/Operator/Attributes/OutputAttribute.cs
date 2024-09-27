using T3.Core.Operator.Slots;

namespace T3.Core.Operator.Attributes;

public class OutputAttribute : OperatorAttribute
{
    public DirtyFlagTrigger DirtyFlagTrigger { get; set; } = DirtyFlagTrigger.None;
}