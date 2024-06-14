#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Resource;
using T3.Serialization;

namespace T3.Core.Audio;

/// <summary>
/// Defines a single audio clip within a timeline.
/// </summary>
public sealed class AudioClip
{
    #region serialized attributes
    public Guid Id;
    public string? FilePath;
    public double StartTime;
    public double EndTime;
    public float Bpm = 120;
    public bool DiscardAfterUse = true;
    public bool IsSoundtrack = false;
    public float Volume = 1.0f;
    #endregion

    /// <summary>
    /// Is initialized after loading...
    /// </summary>
    public double LengthInSeconds;

    public AudioClip()
    {
    }

    #region serialization
    internal static bool TryFromJson(JToken jToken, [NotNullWhen(true)] out AudioClip? newAudioClip)
    {
        var idToken = jToken[nameof(Id)];

        var idString = idToken?.Value<string>();
        if (idString == null || !Guid.TryParse(idString, out var clipId))
        {
            Log.Warning("Missing or malformed id in AudioClip.");
            newAudioClip = null;
            return false;
        }

        
        newAudioClip = new AudioClip
                               {
                                   Id = clipId,
                                   FilePath = jToken[nameof(FilePath)]?.Value<string>(),
                                   StartTime = jToken[nameof(StartTime)]?.Value<double>() ?? 0,
                                   EndTime = jToken[nameof(EndTime)]?.Value<double>() ?? 0,
                                   Bpm = jToken[nameof(Bpm)]?.Value<float>() ?? 0,
                                   DiscardAfterUse = jToken[nameof(DiscardAfterUse)]?.Value<bool>() ?? true,
                                   IsSoundtrack = jToken[nameof(IsSoundtrack)]?.Value<bool>() ?? true,
                                   Volume = jToken[nameof(Volume)]?.Value<float>() ?? 1,
                               };
        
        return true;
    }

    internal void ToJson(JsonTextWriter writer)
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
            if (string.IsNullOrEmpty(FilePath))
            {
                Log.Warning("Empty file path in AudioClip.");
            }
            else
            {
                writer.WriteObject(nameof(FilePath), FilePath);
            }
            if (Math.Abs(Volume - 1.0f) > 0.001f)
                writer.WriteObject(nameof(Volume), Volume);
        }
        writer.WriteEndObject();
    }
    #endregion

    public bool TryGetAbsoluteFilePath(IResourceConsumer instance, [NotNullWhen(true)] out string? absolutePath)
    {
        if (string.IsNullOrWhiteSpace(FilePath))
        {
            absolutePath = null;
            return false;
        }

        if (ResourceManager.TryResolvePath(FilePath, instance, out var path, out _))
        {
            absolutePath = path;
            return true;
        }

        Log.Warning($"Could not resolve path for AudioClip: {FilePath}");
        absolutePath = null;
        return false;
    }
}