using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Animation;
using T3.Core.Logging;

namespace T3.Core.DataTypes
{
    public class Gradient : IEditableInputType
    {
        public virtual void Write(JsonTextWriter writer)
        {
            writer.WritePropertyName(typeof(Gradient).Name);
            writer.WriteStartObject();

            writer.WriteObject("Interpolation", Interpolation);
            writer.WritePropertyName("Steps");
            writer.WriteStartArray();

            foreach (var step in Steps)
            {
                writer.WriteStartObject();
                writer.WriteObject("Id", step.Id);
                writer.WriteValue("NormalizedPosition", step.NormalizedPosition);

                writer.WritePropertyName("Color");
                writer.WriteStartObject();
                writer.WriteValue("R", step.Color.X);
                writer.WriteValue("G", step.Color.Y);
                writer.WriteValue("B", step.Color.Z);
                writer.WriteValue("A", step.Color.W);
                writer.WriteEndObject();

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        public List<Step> Steps = CreateDefaultSteps();
        public Interpolations Interpolation;

        public virtual void Read(JToken inputToken)
        {
            Steps.Clear();
            JToken gradientToken = inputToken[typeof(Gradient).Name];
            if (gradientToken == null)
                return;

            try
            {
                if (inputToken["Interpolation"] != null)
                {
                    Interpolation = (Interpolations)Enum.Parse(typeof(Interpolations), inputToken["Interpolation"].Value<string>());
                }

                foreach (var keyEntry in (JArray)gradientToken["Steps"])
                {
                    Steps.Add(new Step
                                  {
                                      NormalizedPosition = keyEntry["NormalizedPosition"].Value<float>(),
                                      Id = Guid.Parse(keyEntry["Id"].Value<string>()),
                                      Color = new Vector4(keyEntry["Color"]["R"].Value<float>(),
                                                          keyEntry["Color"]["G"].Value<float>(),
                                                          keyEntry["Color"]["B"].Value<float>(),
                                                          keyEntry["Color"]["A"].Value<float>()),
                                  });
                }
            }
            catch (Exception e)
            {
                Log.Warning("Can't read gradient property " + e);
                if (Steps == null || Steps.Count < 1)
                    Steps = CreateDefaultSteps();
            }
        }

        public object Clone() => TypedClone();

        public Gradient TypedClone()
        {
            return new Gradient
                       {
                           Steps = Steps.Select(step => new Step
                                                            {
                                                                NormalizedPosition = step.NormalizedPosition,
                                                                Color = step.Color,
                                                                Id = Guid.NewGuid(),
                                                            })
                                        .ToList(),
                           Interpolation = Interpolation,
                       };
        }

        /// <remarks>
        /// this assumes that steps have been sorted by GradientEditor
        /// </remarks>
        public Vector4 Sample(float t)
        {
            t = t.Clamp(0, 1);
            Step previousStep = null;

            foreach (var step in Steps)
            {
                if (step.NormalizedPosition >= t)
                {
                    if (previousStep == null || previousStep.NormalizedPosition >= step.NormalizedPosition)
                    {
                        return step.Color;
                    }

                    float amount = MathUtils.Remap(t, previousStep.NormalizedPosition, step.NormalizedPosition, 0, 1);

                    return Vector4.Lerp(previousStep.Color, step.Color, amount);
                }

                previousStep = step;
            }

            return previousStep?.Color ?? Vector4.One;
        }

        private static List<Step> CreateDefaultSteps()
        {
            return new List<Step>
                       {
                           new Step
                               {
                                   NormalizedPosition = 0,
                                   Color = new Vector4(1, 0, 1, 1),
                                   Id = Guid.NewGuid(),
                               },
                           new Step
                               {
                                   NormalizedPosition = 1,
                                   Color = new Vector4(0, 0, 1, 1),
                                   Id = Guid.NewGuid(),
                               },
                       };
        }

        public class Step
        {
            public float NormalizedPosition;
            public Vector4 Color;
            public Guid Id;
        }

        public enum Interpolations
        {
            Linear,
            Hold,
            Smooth,
        }
    }
}