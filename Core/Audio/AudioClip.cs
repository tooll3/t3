using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Resource;

namespace T3.Core.Audio;

/// <summary>
/// Defines a single audio clip within a timeline.
/// </summary>
public class AudioClip
{
    #region serialized attributes
    public Guid Id;
    public string FilePath;
    public double StartTime;
    public double EndTime;
    public float Bpm = 120;
    public bool DiscardAfterUse = true;
    public bool IsSoundtrack;
    public float Volume = 1.0f;
    #endregion

    /// <summary>
    /// Is initialized after loading...
    /// </summary>
    public double LengthInSeconds;

    #region serialization
    public static AudioClip FromJson(JToken jToken)
    {
        var idToken = jToken[nameof(Id)];

        var idString = idToken?.Value<string>();
        if (idString == null)
            return null;

        var newAudioClip = new AudioClip
                               {
                                   Id = Guid.Parse(idString),
                                   FilePath = jToken[nameof(FilePath)]?.Value<string>() ?? String.Empty,
                                   StartTime = jToken[nameof(StartTime)]?.Value<double>() ?? 0,
                                   EndTime = jToken[nameof(EndTime)]?.Value<double>() ?? 0,
                                   Bpm = jToken[nameof(Bpm)]?.Value<float>() ?? 0,
                                   DiscardAfterUse = jToken[nameof(DiscardAfterUse)]?.Value<bool>() ?? true,
                                   IsSoundtrack = jToken[nameof(IsSoundtrack)]?.Value<bool>() ?? true,
                                   Volume = jToken[nameof(Volume)]?.Value<float>() ?? 1,
                               };

        return newAudioClip;
    }

    public void ToJson(JsonTextWriter writer)
    {
        //writer.WritePropertyName(Id.ToString());
        writer.WriteStartObject();
        {
            writer.WriteValue(nameof(Id), Id);
            writer.WriteValue(nameof(StartTime), StartTime);
            writer.WriteValue(nameof(EndTime), EndTime);
            writer.WriteValue(nameof(Bpm), Bpm);
            writer.WriteValue(nameof(DiscardAfterUse), DiscardAfterUse);
            writer.WriteValue(nameof(IsSoundtrack), IsSoundtrack);
            writer.WriteObject(nameof(FilePath), FilePath);
            if(Math.Abs(Volume - 1.0f) > 0.001f)
                writer.WriteObject(nameof(Volume), Volume);
        }
        writer.WriteEndObject();
    }
    #endregion
}