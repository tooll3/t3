using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Resource;

namespace T3.Core.DataTypes.DataSet;

public class DataSet
{
    public List<DataChannel> Channels { get; set; } = new();

    public void Clear()
    {
        Channels.Clear();
    }

    public void WriteToFile()
    {
        using var sw = new StreamWriter("dataset.json");
        using var writer = new JsonTextWriter(sw) { Formatting = Formatting.Indented };

        writer.Formatting = Formatting.Indented;
        writer.WriteStartObject();
        writer.WritePropertyName("Channels");
        writer.WriteStartArray();

        foreach (var c in Channels)
        {
            c.WriteToJson(writer);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}

public class DataChannel
{
    public DataChannel(Type type)
    {
        _type = type;

        if (!TypeNameRegistry.Entries.TryGetValue(type, out var typeName))
        {
            throw new Exception("Can't create channel for unregistered value type");
        }

        _typeName = typeName;
    }

    public List<string> Path { get; set; }
    public string Name { get; set; }
    public List<DataEvent> Events { get; set; } = new();
    private readonly Type _type;
    private readonly string _typeName;

    public DataEvent GetLastEvent()
    {
        {
            if (Events == null || Events.Count == 0)
                return null;

            return Events[^1];
        }
    }

    public void WriteToJson(JsonTextWriter writer)
    {
        if (!TypeValueToJsonConverters.Entries.TryGetValue(_type, out var converter))
        {
            Log.Debug($"Can't find converter for type {_type}");
            return;
        }

        writer.WriteStartObject();
        {
            writer.WriteObject("Path", string.Join('/', Path));
            writer.WriteObject("Type", _typeName);

            writer.WritePropertyName("Events");
            writer.WriteStartArray();
            foreach (var dataEvent in Events)
            {
                dataEvent.ToJson(converter, writer);
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }
}

public class DataEvent
{
    public TimeRange TimeRange;
    public double TimeCode;

    public object Value { get; init; }

    public bool IsUnfinished => float.IsInfinity(TimeRange.End);

    public void Finish(float someTime)
    {
        if (!IsUnfinished)
        {
            Log.Warning("setting finish time of fished note?");
        }

        TimeRange.End = someTime;
    }

    public void ToJson(Action<JsonTextWriter, object> converter, JsonTextWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteValue("TimeCode", TimeCode);
        writer.WritePropertyName("Value");
        converter(writer, Value);
        writer.WriteEndObject();
    }
}

// public class FloatDataEvent : DataEvent
// {
//     // public float FloatValue { get; set; }
//     //public override object Value { get; set; }
// }