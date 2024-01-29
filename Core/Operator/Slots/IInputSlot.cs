using System;
using System.Collections.Generic;

namespace T3.Core.Operator.Slots
{
    public interface IInputSlot : ISlot
    {
        SymbolChild.Input Input { get; set; }
        Type MappedType { get; set; }
        bool IsMultiInput { get; }
        void RestoreUpdateAction();
        bool TryGetAsMultiInput(out IMultiInputSlot multiInput);
    }
}