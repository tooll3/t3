using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Logging;
using T3.Core.Operator.Slots;

namespace T3.Core.Animation
{
    public class TimeClip : IOutputData, ITimeClip
    {
        public Guid Id { get; set; }

        private TimeRange _timeRange = new TimeRange(0.0f, 4.0f);
        public ref TimeRange TimeRange => ref _timeRange;

        private TimeRange _sourceRange = new TimeRange(0.0f, 4.0f);
        public ref TimeRange SourceRange => ref _sourceRange;

        public string Name { get; set; } = string.Empty;
        public int LayerIndex { get; set; } = 0;

        public Type DataType => typeof(TimeClip);

        public void ToJson(JsonTextWriter writer)
        {
            writer.WritePropertyName("TimeClip");
            writer.WriteStartObject();
            writer.WritePropertyName("TimeRange");
            writer.WriteStartObject();
            writer.WriteValue("Start", _timeRange.Start);
            writer.WriteValue("End", _timeRange.End);
            writer.WriteEndObject();
            writer.WritePropertyName("SourceRange");
            writer.WriteStartObject();
            writer.WriteValue("Start", _sourceRange.Start);
            writer.WriteValue("End", _sourceRange.End);
            writer.WriteEndObject();
            writer.WriteValue("LayerIndex", LayerIndex);
            writer.WriteEndObject();
        }

        public void ReadFromJson(JToken json)
        {
            var timeClip = json["TimeClip"];
            if (timeClip != null)
            {
                var timeRange = timeClip["TimeRange"];
                if (timeRange != null)
                {
                    _timeRange = new TimeRange(timeRange["Start"].Value<float>(), timeRange["End"].Value<float>());
                }

                var sourceRange = timeClip["SourceRange"];
                if (sourceRange != null)
                {
                    _sourceRange = new TimeRange(sourceRange["Start"].Value<float>(), sourceRange["End"].Value<float>());
                }

                LayerIndex = timeClip["LayerIndex"]?.Value<int>() ?? 0;
            }
        }

        public bool Assign(IOutputData outputData)
        {
            if (outputData is TimeClip otherTimeClip)
            {
                _timeRange = otherTimeClip.TimeRange;
                _sourceRange = otherTimeClip.SourceRange;
                LayerIndex = otherTimeClip.LayerIndex;

                return true;
            }

            Log.Error($"Trying to assign output data of type '{outputData.GetType()}' to 'TimeClip'.");

            return false;
        }
    }
}