using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Logging;

namespace T3.Core.DataTypes
{
    public class Gradient
    {
        public List<Step> Steps = CreateDefaultSteps();

        public virtual void Write(JsonTextWriter writer)
        {
            writer.WritePropertyName(typeof(Gradient).Name);
            writer.WriteStartObject();

            writer.WritePropertyName("Steps");
            writer.WriteStartArray();

            foreach (var step in Steps)
            {
                writer.WriteStartObject();

                writer.WriteValue("NormalizedPosition", step.NormalizedPosition);

                writer.WritePropertyName("Color");
                writer.WriteStartObject();
                writer.WriteValue("R", step.Color.X);
                writer.WriteValue("G", step.Color.Y);
                writer.WriteValue("B", step.Color.Z);
                writer.WriteValue("A", step.Color.W);
                writer.WriteEndObject();

                writer.WriteObject("Interpolation", step.Interpolation);
                writer.WriteObject("Id", step.Id);

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        public virtual void Read(JToken inputToken)
        {
            JToken gradientToken = inputToken[typeof(Gradient).Name];
            if (gradientToken == null)
                return;

            try
            {
                foreach (var keyEntry in (JArray)gradientToken["Steps"])
                {
                    Steps.Add(new Step()
                                  {
                                      Interpolation = (Interpolations)Enum.Parse(typeof(Interpolations), keyEntry["Interpolation"].Value<string>()),
                                      NormalizedPosition = keyEntry["NormalizedPosition"].Value<float>(),
                                      Id = keyEntry["Id"].Value<Guid>(),
                                      Color = new Vector4(
                                                          keyEntry["Color"]["R"].Value<float>(),
                                                          keyEntry["Color"]["G"].Value<float>(),
                                                          keyEntry["Color"]["B"].Value<float>(),
                                                          keyEntry["Color"]["A"].Value<float>()
                                                         ),
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

        public Gradient Clone()
        {
            return new Gradient()
                       {
                           Steps = new List<Step>(Steps) //FIXME: this should also create new ids for steps
                       };
        }

        private static List<Step> CreateDefaultSteps()
        {
            return new List<Step>()
                       {
                           new Step()
                               {
                                   NormalizedPosition = 0,
                                   Color = new Vector4(1, 0, 1, 1),
                                   Id = Guid.NewGuid(),
                               },
                           new Step()
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
            public Interpolations Interpolation;
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