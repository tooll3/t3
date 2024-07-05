using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Resource;

namespace T3.Core.DataTypes.DataSet;

/// <summary>
/// Defines a set of <see cref="DataChannel"/> event channels. 
/// </summary>
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
    public List<DataEvent> Events { get; set; } = new(100);
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
            lock (Events)
            {
                foreach (var dataEvent in Events.ToList())
                {
                    dataEvent.ToJson(converter, writer);
                }
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }
    
    public int FindHighestIndexBelowTime(double time)
    {
        if (Events.Count == 0)
            return -1;
        
        var lastIndex = Events.Count - 1;
        if (Events[lastIndex].Time <= time)
            return lastIndex;
        
        var firstIndex = 0;
        while (lastIndex - firstIndex > 1)
        {
            var middleIndex = (firstIndex + lastIndex) / 2;
            if (Events[middleIndex].Time <= time)
                firstIndex = middleIndex;
            else
                lastIndex = middleIndex;
        }
        return firstIndex;
    }
}

public class DataEvent
{
    public double Time;
    public double TimeCode;

    public object Value { get; set; }

    public virtual void ToJson(Action<JsonTextWriter, object> converter, JsonTextWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteValue("TimeCode", TimeCode);
        writer.WritePropertyName("Value");
        converter(writer, Value);
        writer.WriteEndObject();
    }

    public bool TryGetNumericValue(out double v)
    {
        switch (Value)
        {
            case float f: v = f; break;
            case double d: v = d;break;
            case int i: v = i; break;
            default: v= double.NaN; return false;
        }

        return !double.IsNaN(v);
    }
}

public class DataIntervalEvent :DataEvent
{
    public double EndTime = double.PositiveInfinity;
    
    public bool IsUnfinished => double.IsInfinity(EndTime);

    public void Finish(double someTime)
    {
        if (!IsUnfinished)
        {
            Log.Warning($"setting finish time of fished note? {EndTime} vs {someTime}");
        }

        EndTime = someTime;
    }

    public override void ToJson(Action<JsonTextWriter, object> converter, JsonTextWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteValue("TimeCode", TimeCode);
        writer.WriteValue("Time", Time);
        writer.WriteValue("EndTime", EndTime);
        writer.WritePropertyName("Value");
        converter(writer, Value);
        writer.WriteEndObject();
    }
}