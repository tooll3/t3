using System;

namespace T3.Core.Operator.Slots
{
    public interface IInputSlot : ISlot
    {
        SymbolChild.Input Input { get; set; }
        bool IsMultiInput { get; }
        Type MappedType { get; set; }

        void SetUpdateActionBackToDefault();
    }
}