using System;
using T3.Core.Operator;
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
        public bool IsSelected { get; set; }

        public void DrawValue(ISlot slot, bool recompute)
        {
            if (recompute)
            {
                StartInvalidation(slot);
                _evaluationContext.Reset();
                slot.Update(_evaluationContext);
            }

            DrawTypedValue(slot);
        }

        protected abstract void DrawTypedValue(ISlot slot);

        public void StartInvalidation(ISlot slot)
        {
            DirtyFlag.InvalidationRefFrame++;
            Invalidate(slot);
        }
        
        private int Invalidate(ISlot slot)
        {
            if (slot is IInputSlot)
            {
                if (slot.IsConnected)
                {
                    slot.DirtyFlag.Target = Invalidate(slot.GetConnection(0));
                }
                else if (slot.DirtyFlag.Trigger != DirtyFlagTrigger.None)
                {
                    slot.DirtyFlag.Invalidate();
                }
            }
            else if (slot.IsConnected)
            {
                // slot is an output of an composition op
                slot.DirtyFlag.Target = Invalidate(slot.GetConnection(0));
            }
            else
            {
                Instance parent = slot.Parent;

                bool outputDirty = false;
                foreach (var input in parent.Inputs)
                {
                    if (input.IsConnected)
                    {
                        if (input.IsMultiInput)
                        {
                            var multiInput = (IMultiInputSlot)input;
                            int dirtySum = 0;
                            foreach (var entry in multiInput.GetCollectedInputs())
                            {
                                dirtySum += Invalidate(entry);
                            }

                            input.DirtyFlag.Target = dirtySum;
                        }
                        else
                        {
                            input.DirtyFlag.Target = Invalidate(input.GetConnection(0));
                        }
                    }
                    else if (input.DirtyFlag.Trigger != DirtyFlagTrigger.None)
                    {
                        input.DirtyFlag.Invalidate();
                    }

                    outputDirty |= input.DirtyFlag.IsDirty;
                }

                if (outputDirty || slot.DirtyFlag.Trigger != DirtyFlagTrigger.None)
                {
                    slot.DirtyFlag.Invalidate();
                }
            }

            return slot.DirtyFlag.Target;
        }

        private readonly EvaluationContext _evaluationContext = new EvaluationContext();
    }
}