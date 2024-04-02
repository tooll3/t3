
using System.Collections.Generic;

namespace T3.Core.Operator.Slots;

public interface IExtractedInput<T>
{
    public Slot<T> OutputSlot { get; }
    public void SetTypedInputValuesTo(T value, out IEnumerable<IInputSlot> changedSlots);
}