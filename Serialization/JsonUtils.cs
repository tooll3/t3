using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Logging;

namespace T3.Serialization;

public static class JsonUtils
{
    public static T ReadEnum<T>(JToken o, string name) where T : struct, Enum
    {
        var dirtyFlagJson = o[name];
        return dirtyFlagJson != null
                   ? Enum.Parse<T>(dirtyFlagJson.Value<string>() ?? string.Empty)
                   : default;
    }

    public static T? ReadToken<T>(JToken o, string name, T? defaultValue = default)
    {
        var jSettingsToken = o[name];
        return jSettingsToken == null ? defaultValue : jSettingsToken.Value<T>();
    }

    public static T? TryLoadingJson<T>(string filepath) where T : class, new()
    {
        if (!File.Exists(filepath))
        {
            Log.Warning($"{filepath} doesn't exist yet");
            return null;
        }

        var jsonBlob = File.ReadAllText(filepath);
        var serializer = JsonSerializer.Create();
        var fileTextReader = new StringReader(jsonBlob);
        try
        {
            if (serializer.Deserialize(fileTextReader, typeof(T)) is T configurations)
                return configurations;
        }
        catch (Exception e)
        {
            Log.Error($"Can't load {filepath}:" + e.Message);
            return null;
        }

        Log.Error($"Can't load {filepath}");
        return null;
    }

    public static void SaveJson<T>(T dataObject, string filepath) where T : class, new()
    {
        if (string.IsNullOrEmpty(filepath))
        {
            Log.Warning($"Can't save {typeof(T)} to empty filename...");
            return;
        }

        var serializer = JsonSerializer.Create();
        serializer.Formatting = Formatting.Indented;
        try
        {
            using var streamWriter = File.CreateText(filepath);
            serializer.Serialize(streamWriter, dataObject);
        }
        catch (Exception e)
        {
            Log.Warning($"Can't create file {filepath} to save {typeof(T)} " + e.Message);
        }
    }

    public static void WriteValue<T>(this JsonTextWriter writer, string name, T value) where T : struct
    {
        writer.WritePropertyName(name);
        writer.WriteValue(value);
    }

    public static void WriteObject(this JsonTextWriter writer, string name, object value)
    {
        writer.WritePropertyName(name);
        writer.WriteValue(value.ToString());
    }
}