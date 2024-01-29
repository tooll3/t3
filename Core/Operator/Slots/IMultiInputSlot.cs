using System.Collections.Generic;

namespace T3.Core.Operator.Slots
{
    public interface IMultiInputSlot : IInputSlot
    {
        IEnumerable<ISlot> GetCollectedInputs();
        List<int> LimitMultiInputInvalidationToIndices { get; }
    }
}