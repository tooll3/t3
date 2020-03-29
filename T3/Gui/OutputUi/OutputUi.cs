using System;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Selection;
using Vector2 = System.Numerics.Vector2;

namespace T3.Gui.OutputUi
{
    public abstract class OutputUi<T> : IOutputUi
    {
        public Symbol.OutputDefinition OutputDefinition { get; set; }
        public Guid Id => OutputDefinition.Id;
        public Type Type { get; } = typeof(T);
        public Vector2 PosOnCanvas { get; set; } = Vector2.Zero;
        public Vector2 Size { get; set; } = new Vector2(100, 30);
        // public bool IsSelected
        // {
        //     get { return SelectionManager.IsNodeSelected(this);} 
        //     set  }
        public bool IsSelected => SelectionManager.IsNodeSelected(this);
        public abstract IOutputUi Clone();

        public void DrawValue(ISlot slot, EvaluationContext context, bool recompute)
        {
            _evaluationContext = context;
            if (recompute)
            {
                StartInvalidation(slot);
                slot.Update(_evaluationContext);
            }

            DrawTypedValue(slot);
        }

        protected abstract void DrawTypedValue(ISlot slot);

        public void StartInvalidation(ISlot slot)
        {
            DirtyFlag.InvalidationRefFrame++;
            slot.Invalidate();
        }

        private EvaluationContext _evaluationContext;
    }
}