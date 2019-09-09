using System;
using System.Collections.Generic;
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
                newCurve.AddOrUpdateV(EvaluationContext.GlobalTime + 1, new VDefinition()
                                                                        {
                                                                            Value = typedInputSlot.Value + 2,
                                                                            InType = VDefinition.Interpolation.Spline,
                                                                            OutType = VDefinition.Interpolation.Spline,
                                                                        });
                _animatedInputCurves.Add(inputSlot.Id, newCurve);

                typedInputSlot.UpdateAction = context => { typedInputSlot.Value = (float)newCurve.GetSampledValue(context.Time); };
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
                typedInputSlot.SetUpdateActionBackToDefault();

                _animatedInputCurves.Remove(inputSlot.Id);
            }
        }

        public bool IsInputSlotAnimated(IInputSlot inputSlot)
        {
            return _animatedInputCurves.ContainsKey(inputSlot.Id);
        }

        public Curve GetCurveForInput(IInputSlot inputSlot)
        {
            return _animatedInputCurves[inputSlot.Id];
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

                writer.WriteValue("InputId", entry.Key);
                entry.Value.Write(writer); // write curve itself

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        public void Read(JToken inputToken)
        {
            foreach (JToken entry in inputToken)
            {
                Guid inputId = Guid.Parse(entry["InputId"].Value<string>());

                Curve curve = new Curve();
                curve.Read(entry);

                _animatedInputCurves.Add(inputId, curve);
            }
        }

        private readonly Dictionary<Guid, Curve> _animatedInputCurves = new Dictionary<Guid, Curve>();
    }
}