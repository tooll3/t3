#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Audio;
using T3.Serialization;
using T3.Core.Resource;

namespace T3.Core.Operator;

/// <summary>
/// Defines playback related settings for a symbol like its primary soundtrack or audio input device,
/// BPM rate and other settings.
/// </summary>
///  todo - treat AudioClips the same way as timeline clips - soundtracks might be a special case
public sealed class PlaybackSettings
{
    public bool Enabled { get; set; }
    public float Bpm = 120;
    public List<AudioClipDefinition> AudioClips { get; private init; } = [];
    public AudioSources AudioSource;
    public SyncModes Syncing;

    public string AudioInputDeviceName = string.Empty;
    public float AudioGainFactor = 1;
    public float AudioDecayFactor = 0.9f;

    public PlaybackSettings()
    {
    }

    public bool GetMainSoundtrack(IResourceConsumer? instance, [NotNullWhen(true)] out AudioClipResourceHandle? soundtrack)
    {
        foreach (var clip in AudioClips)
        {
            if (!clip.IsSoundtrack)
                continue;

            soundtrack = new AudioClipResourceHandle(clip, instance);
            return true;
        }

        soundtrack = null;
        return false;
    }

    public enum AudioSources
    {
        ProjectSoundTrack,
        ExternalDevice,
    }

    public enum SyncModes
    {
        Timeline,
        Tapping,
    }

    internal void WriteToJson(JsonTextWriter writer)
    {
        var hasSettingsForClips = Enabled || AudioClips.Count > 0;
        if (!hasSettingsForClips)
            return;

        writer.WritePropertyName(nameof(PlaybackSettings));

        writer.WriteStartObject();
        {
            //writer.WriteEndArray();

            writer.WriteValue(nameof(Enabled), Enabled);
            writer.WriteValue(nameof(Bpm), Bpm);
            writer.WriteValue(nameof(AudioSource), AudioSource);
            writer.WriteValue(nameof(Syncing), Syncing);
            writer.WriteValue(nameof(AudioDecayFactor), AudioDecayFactor);
            writer.WriteValue(nameof(AudioGainFactor), AudioGainFactor);
            writer.WriteObject(nameof(AudioInputDeviceName), AudioInputDeviceName);

            // Write audio clips
            if (AudioClips.Count != 0)
            {
                writer.WritePropertyName("AudioClips");
                writer.WriteStartArray();
                foreach (var audioClip in AudioClips)
                {
                    audioClip.ToJson(writer);
                }

                writer.WriteEndArray();
            }
        }

        writer.WriteEndObject();
    }

    internal static PlaybackSettings? ReadFromJson(JToken o)
    {
        var clips = GetClips(o).ToList(); // Support legacy json format
        if (clips.Count == 0)
            return null;

        var playbackSettingsName = nameof(Symbol.PlaybackSettings);
        var jToken = o[playbackSettingsName];
        if (jToken == null)
            return null;

        var settingsToken = (JObject)jToken;

        var newSettings = new PlaybackSettings
                              {
                                  AudioClips = clips,
                              };

        newSettings.Enabled = JsonUtils.ReadToken(settingsToken, nameof(Enabled), false);
        newSettings.Bpm = JsonUtils.ReadToken(settingsToken, nameof(Bpm), 120f);
        newSettings.AudioSource = JsonUtils.ReadEnum<AudioSources>(settingsToken, nameof(AudioSource));
        newSettings.Syncing = JsonUtils.ReadEnum<SyncModes>(settingsToken, nameof(Syncing));
        newSettings.AudioDecayFactor = JsonUtils.ReadToken(settingsToken, nameof(AudioDecayFactor), 0.5f);
        newSettings.AudioGainFactor = JsonUtils.ReadToken(settingsToken, nameof(AudioGainFactor), 1f);
        
        
        newSettings.AudioInputDeviceName = JsonUtils.ReadToken<string>(settingsToken, nameof(AudioInputDeviceName), null)?? string.Empty;

        newSettings.AudioClips.AddRange(GetClips(settingsToken)); // Support correct format

        if (newSettings.Bpm == 0)
        {
            var soundtrack = newSettings.AudioClips.FirstOrDefault(c => c.IsSoundtrack);
            if (soundtrack != null)
            {
                newSettings.Bpm = soundtrack.Bpm;
                newSettings.Enabled = true;
            }
        }

        return newSettings;
    }

    private static IEnumerable<AudioClipDefinition> GetClips(JToken o)
    {
        var jToken = o[nameof(Symbol.PlaybackSettings.AudioClips)];
        if(jToken == null)
            yield break;
        
        var jAudioClipArray = (JArray)jToken;
        foreach (var c in jAudioClipArray)
        {
            if (AudioClipDefinition.TryFromJson(c, out var clip))
            {
                yield return clip;
            }
        }
    }
}