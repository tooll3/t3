using System;
using System.Collections.Generic;
using T3.Core.Animation.Curve;
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

                var newCurve = new Curve();
                newCurve.AddOrUpdateV(EvaluationContext.GlobalTime, new VDefinition()
                {
                    Value = typedInputSlot.Value,
                    InType = VDefinition.Interpolation.Spline,
                    OutType = VDefinition.Interpolation.Spline,
                });
                newCurve.AddOrUpdateV(EvaluationContext.GlobalTime + 1, new VDefinition()
                {
                    Value = typedInputSlot.Value + 2,
                    InType = VDefinition.Interpolation.Spline,
                    OutType = VDefinition.Interpolation.Spline,
                });
                AnimatedInputCurves.Add(inputSlot, newCurve);

                typedInputSlot.UpdateAction = context =>
                                              {
                                                  typedInputSlot.Value = (float)newCurve.GetSampledValue(context.Time);
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
                AnimatedInputCurves.Remove(inputSlot);
            }
        }

        public bool IsInputSlotAnimated(IInputSlot inputSlot)
        {
            return AnimatedInputs.ContainsKey(inputSlot);
        }


        public Dictionary<IInputSlot, Action<EvaluationContext>> AnimatedInputs { get; } = new Dictionary<IInputSlot, Action<EvaluationContext>>();
        public Dictionary<IInputSlot, Curve> AnimatedInputCurves { get; } = new Dictionary<IInputSlot, Curve>();
    }
}