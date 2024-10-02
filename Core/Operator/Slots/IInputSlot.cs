using System;
using System.Collections.Generic;

namespace T3.Core.Operator.Slots
{
    public interface IInputSlot : ISlot
    {
        SymbolChild.Input Input { get; set; }
        bool IsMultiInput { get; }
        Type MappedType { get; set; }
        List<int> LimitMultiInputInvalidationToIndices { get; set; }

        void RestoreUpdateAction();
    }
}