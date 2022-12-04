using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Logging;
using T3.Core.Resource;
using T3.Core.Utils;
using Vector4 = System.Numerics.Vector4;

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

            Step[] stepsToWrite;
            lock (Steps)
            {
                stepsToWrite = Steps.Select(step => new Step(step)).ToArray();
            }

            foreach (var step in stepsToWrite)
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

        public List<Step> Steps { get; set; } = new List<Step>();
        public Interpolations Interpolation { get; set; }
        
        public virtual void Read(JToken inputToken)
        {
            Steps.Clear();
            JToken gradientToken = inputToken[typeof(Gradient).Name];
            if (gradientToken == null)
                return;

            try
            {
                if (gradientToken["Interpolation"] != null)
                {
                    Interpolation = (Interpolations)Enum.Parse(typeof(Interpolations), gradientToken["Interpolation"].Value<string>());
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
                        
                        float amount = 0;   // Hold
                        switch (Interpolation)
                        {
                            case Interpolations.Linear:
                                amount = MathUtils.RemapAndClamp(t, previousStep.NormalizedPosition, step.NormalizedPosition, 0, 1);
                                break;
                            case Interpolations.Smooth:
                                amount = MathUtils.RemapAndClamp(t, previousStep.NormalizedPosition, step.NormalizedPosition, 0, 1);
                                amount = MathUtils.SmootherStep(0, 1, amount);
                                break;
                            
                            case Interpolations.OkLab:
                                amount = MathUtils.RemapAndClamp(t, previousStep.NormalizedPosition, step.NormalizedPosition, 0, 1);
                                return OkLab.Mix(previousStep.Color, step.Color, amount);
                        }

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

            public Step() { }

            /// <summary>
            /// Constructor to clone the provided <see cref="Step"/>
            /// </summary>
            /// <param name="original"></param>
            public Step(Step original)
            {
                NormalizedPosition = original.NormalizedPosition;
                Color = original.Color;
                Id = original.Id;
            }
        }

        public enum Interpolations
        {
            Linear,
            Hold,
            Smooth,
            OkLab
        }
    }
}