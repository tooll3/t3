using System;
using System.Collections.Generic;
using T3.Core.Logging;

namespace T3.Core.Operator.Types
{
    public static class Animator
    {
        public static void CreateInputUpdateAction<T>(IInputSlot inputSlot)
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

        public static void RemoveAnimationFrom(IInputSlot inputSlot)
        {
            if (inputSlot is Slot<float> typedInputSlot)
            {
                typedInputSlot.UpdateAction = AnimatedInputs[inputSlot]; // restore previous update action
                AnimatedInputs.Remove(inputSlot);
            }
        }

        public static bool IsInputSlotAnimated(IInputSlot inputSlot)
        {
            return AnimatedInputs.ContainsKey(inputSlot);
        }

        private static Dictionary<IInputSlot, Action<EvaluationContext>> AnimatedInputs { get; } = new Dictionary<IInputSlot, Action<EvaluationContext>>();
    }
}