
namespace T3.Core.Operator.Slots;

public interface IExtractedInput<T>
{
    public Slot<T> OutputSlot { get; }
    public void SetInputValues(T value);
}