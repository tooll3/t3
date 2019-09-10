using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Animation.Curves;
using T3.Core.Logging;

namespace T3.Core.Operator
{
    public class SymbolExtension
    {
        // todo: how is a symbol extension defined, what exactly does this mean
    }

    public class Animator : SymbolExtension
    {
        struct CurveId
        {
            public CurveId(Guid instanceId, Guid inputId)
            {
                InstanceId = instanceId;
                InputId = inputId;
            }

            public CurveId(IInputSlot inputSlot)
            {
                InstanceId = inputSlot.Parent.Id;
                InputId = inputSlot.Id;
            }

            public readonly Guid InstanceId;
            public readonly Guid InputId;
        }

        public void CreateInputUpdateAction<T>(IInputSlot inputSlot)
        {
            if (inputSlot is Slot<float> typedInputSlot)
            {
                var newCurve = new Curve();
                newCurve.AddOrUpdateV(EvaluationContext.GlobalTime, new VDefinition()
                                                                    {
                                                                        Value = typedInputSlot.Value,
                                                                        InType = VDefinition.Interpolation.Spline,
                                                                        OutType = VDefinition.Interpolation.Spline,
                                                                    });
                _animatedInputCurves.Add(new CurveId(inputSlot), newCurve);
                newCurve.AddOrUpdateV(EvaluationContext.GlobalTime + 1, new VDefinition()
                                                                        {
                                                                            Value = typedInputSlot.Value + 2,
                                                                            InType = VDefinition.Interpolation.Spline,
                                                                            OutType = VDefinition.Interpolation.Spline,
                                                                        });

                typedInputSlot.UpdateAction = context =>
                                              {
                                                  typedInputSlot.Value = (float)newCurve.GetSampledValue(context.Time);
                                              };
                typedInputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
            }
            else
            {
                Log.Error("Could not create update action.");
            }
        }

        internal void CreateUpdateActionsForExistingCurves(Instance compositionInstance)
        {
            // gather all inputs that correspond to stored ids
            var relevantInputs = from curveEntry in _animatedInputCurves
                                 from childInstance in compositionInstance.Children
                                 where curveEntry.Key.InstanceId == childInstance.Id
                                 from inputSlot in childInstance.Inputs
                                 where curveEntry.Key.InputId == inputSlot.Id
                                 select (inputSlot, curveEntry.Value);

            foreach (var entry in relevantInputs)
            {
                var (inputSlot, curve) = entry;
                if (inputSlot is Slot<float> typedInputSlot)
                {
                    typedInputSlot.UpdateAction = context =>
                                                  {
                                                      typedInputSlot.Value = (float)curve.GetSampledValue(context.Time);
                                                  };
                    typedInputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                }
            }
        }

        public void RemoveAnimationFrom(IInputSlot inputSlot)
        {
            if (inputSlot is Slot<float> typedInputSlot)
            {
                typedInputSlot.SetUpdateActionBackToDefault();
                typedInputSlot.DirtyFlag.Trigger &= ~DirtyFlagTrigger.Animated;

                _animatedInputCurves.Remove(new CurveId(inputSlot));
            }
        }

        public bool IsInputSlotAnimated(IInputSlot inputSlot)
        {
            return _animatedInputCurves.ContainsKey(new CurveId(inputSlot));
        }

        public Curve GetCurveForInput(IInputSlot inputSlot)
        {
            return _animatedInputCurves[new CurveId(inputSlot)];
        }

        public void Write(JsonTextWriter writer)
        {
            if (_animatedInputCurves.Count == 0)
                return;

            writer.WritePropertyName("Animator");
            writer.WriteStartArray();

            foreach (var entry in _animatedInputCurves)
            {
                writer.WriteStartObject();

                writer.WriteValue("InstanceId", entry.Key.InstanceId);
                writer.WriteValue("InputId", entry.Key.InputId);
                entry.Value.Write(writer); // write curve itself

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        public void Read(JToken inputToken)
        {
            foreach (JToken entry in inputToken)
            {
                Guid instanceId = Guid.Parse(entry["InstanceId"].Value<string>());
                Guid inputId = Guid.Parse(entry["InputId"].Value<string>());

                Curve curve = new Curve();
                curve.Read(entry);

                _animatedInputCurves.Add(new CurveId(instanceId, inputId), curve);
            }
        }

        private readonly Dictionary<CurveId, Curve> _animatedInputCurves = new Dictionary<CurveId, Curve>();
    }
}