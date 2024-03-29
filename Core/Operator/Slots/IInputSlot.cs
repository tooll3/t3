using System;

namespace T3.Core.Operator.Slots
{
    public interface IInputSlot : ISlot
    {
        Symbol.Child.Input Input { get; set; }
        Type MappedType { get; set; }
        bool IsMultiInput { get; }
        bool IsDirty { get; }
        void RestoreUpdateAction();
        bool TryGetAsMultiInput(out IMultiInputSlot multiInput);
        void SetVisited();
    }
}