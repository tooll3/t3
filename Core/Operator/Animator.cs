using System;
using System.Collections.Generic;
using T3.Core.Logging;

namespace T3.Core.Operator
{
    public class SymbolExtension
    {
        // todo: how is a symbol extension defined, what exactly does this mean
    }
    
    public class Animator : SymbolExtension
    {
        public void CreateInputUpdateAction<T>(IInputSlot inputSlot)
        {
            if (inputSlot is Slot<float> typedInputSlot)
            {
                AnimatedInputs.Add(inputSlot, typedInputSlot.UpdateAction);
                typedInputSlot.UpdateAction = context =>
                                              {
                                                  typedInputSlot.Value = (float)Math.Cos(context.Time);
                                              };
            }
            else
            {
                Log.Error("Could not create update action.");
            }
        }

        public void RemoveAnimationFrom(IInputSlot inputSlot)
        {
            if (inputSlot is Slot<float> typedInputSlot)
            {
                typedInputSlot.UpdateAction = AnimatedInputs[inputSlot]; // restore previous update action
                AnimatedInputs.Remove(inputSlot);
            }
        }

        public bool IsInputSlotAnimated(IInputSlot inputSlot)
        {
            return AnimatedInputs.ContainsKey(inputSlot);
        }

        private Dictionary<IInputSlot, Action<EvaluationContext>> AnimatedInputs { get; } = new Dictionary<IInputSlot, Action<EvaluationContext>>();
    }
}