using System;
using T3.Core.Logging;

namespace T3.Core.Operator.Slots
{
    public class IterationOutputSlot<T> : Slot<T>
    {
        private void UpdateForIteration(EvaluationContext context)
        {
            // get corresponding iteration input

            // get all ops in corresponding composition op
            // foreach (var child in Parent.Children)
            // {
            // Log.Debug($"{child.Symbol.Name}");
            // }

            _baseUpdateAction(context);
        }

        private Action<EvaluationContext> _baseUpdateAction;

        public override Action<EvaluationContext> UpdateAction
        {
            set
            {
                _baseUpdateAction = value;
                base.UpdateAction = UpdateForIteration;
            }
        }
    }

    public class IterationInputSlot<T> : InputSlot<T>
    {
    }
}