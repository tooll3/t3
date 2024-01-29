using System;

namespace T3.Core.Operator.Slots
{
    public interface ISlot
    {
        Guid Id { get; set; }
        Type ValueType { get; }
        Instance Parent { get; set; }
        DirtyFlag DirtyFlag { get; }
        int Invalidate();
        void Update(EvaluationContext context);
        bool IsDisabled { set; }
        void AddConnection(ISlot source, int index = 0);
        void RemoveConnection(int index = 0);
        bool IsConnected { get; }
        ISlot GetConnection(int index);
    }
}