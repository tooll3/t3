using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Logging;
using T3.Core.Resource;

namespace T3.Core.Model;

public class JsonFileResult<T>
{
    public readonly JToken JToken;
    public readonly string FilePath;

    /// <summary>
    /// Can be used instead of a null check, so long as you are certain
    /// that you never explicitly set the value to `null`
    /// </summary>
    public bool ObjectWasSet { get; private set; }

    private T _object;

    public T Object
    {
        get => _object;
        set
        {
            _object = value;
                
            if(value is not null)
                ObjectWasSet = true;
        }
    }

    public Guid Guid { get; }

    private JsonFileResult(JToken jToken, string filePath, Guid guid)
    {
        JToken = jToken;
        FilePath = filePath;
        Guid = guid;
    }

    public static JsonFileResult<T> ReadAndCreate(string filePath)
    {
        using var streamReader = new StreamReader(filePath);
        using var jsonReader = new JsonTextReader(streamReader);

        //Log.Debug($"Loading {filePath}");

        try
        {
            var jToken = JToken.ReadFrom(jsonReader, SymbolJson.LoadSettings);
            var keyToken = jToken[SymbolJson.JsonKeys.Id];

            if (keyToken is null)
            {
                throw new JsonException($"Guid \"{SymbolJson.JsonKeys.Id}\" not found");
            }
                
            var guid = Guid.Parse(keyToken.Value<string>());
            return new JsonFileResult<T>(jToken, filePath, guid);
        }
        catch (Exception e)
        {
            Log.Error($"Error reading json from {filePath}:\n{e}");
            throw;
        }
    }
}