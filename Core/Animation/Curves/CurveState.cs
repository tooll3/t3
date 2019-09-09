using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace T3.Core.Animation.Curves
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
        }

        private Utils.OutsideCurveBehavior _preCurveMapping;
        private Utils.OutsideCurveBehavior _postCurveMapping;
    }
}