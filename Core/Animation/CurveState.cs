using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.DataTypes;
using T3.Core.Model;
using T3.Core.Resource;
using T3.Serialization;

namespace T3.Core.Animation
{
    public class CurveState
    {
        public SortedList<double, VDefinition> Table { get; set; }

        public Utils.OutsideCurveBehavior PreCurveMapping
        {
            get => _preCurveMapping;
            set
            {
                _preCurveMapping = value;
                PreCurveMapper = Utils.CreateOutsideCurveMapper(value);
            }
        }
        public Utils.OutsideCurveBehavior PostCurveMapping
        {
            get => _postCurveMapping;
            set
            {
                _postCurveMapping = value;
                PostCurveMapper = Utils.CreateOutsideCurveMapper(value);
            }
        }

        public IOutsideCurveMapper PreCurveMapper { get; private set; }
        public IOutsideCurveMapper PostCurveMapper { get; private set; }

        public CurveState()
        {
            Table = new SortedList<double, VDefinition>();
            PreCurveMapping = Utils.OutsideCurveBehavior.Constant;
            PostCurveMapping = Utils.OutsideCurveBehavior.Constant;
        }

        public CurveState Clone()
        {
            var clone = new CurveState {PreCurveMapping = _preCurveMapping, PostCurveMapping = _postCurveMapping};

            foreach (var point in Table)
                clone.Table[point.Key] = point.Value.Clone();

            return clone;
        }

        public virtual void Write(JsonTextWriter writer)
        {
            writer.WritePropertyName("Curve");
            writer.WriteStartObject();

            writer.WriteObject("PreCurve", PreCurveMapping);
            writer.WriteObject("PostCurve", PostCurveMapping);

            // write keys
            writer.WritePropertyName("Keys");
            writer.WriteStartArray();

            foreach (var point in Table)
            {
                writer.WriteStartObject();

                writer.WriteValue("Time", point.Key);
                point.Value.Write(writer);

                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        public virtual void Read(JToken inputToken)
        {
            JToken curveToken = inputToken["Curve"];
            if (curveToken == null)
                return;

            PreCurveMapping = (Utils.OutsideCurveBehavior)Enum.Parse(typeof(Utils.OutsideCurveBehavior), curveToken["PreCurve"].Value<string>());
            PostCurveMapping = (Utils.OutsideCurveBehavior)Enum.Parse(typeof(Utils.OutsideCurveBehavior), curveToken["PostCurve"].Value<string>());

            foreach (var keyEntry in (JArray) curveToken["Keys"])
            {
                var time = keyEntry["Time"].Value<double>();
                time = Math.Round(time, Curve.TIME_PRECISION);
                var key = new VDefinition();
                key.Read(keyEntry);
                key.U = time;
                Table.Add(time, key);
            }
        }

        private Utils.OutsideCurveBehavior _preCurveMapping;
        private Utils.OutsideCurveBehavior _postCurveMapping;
    }
}