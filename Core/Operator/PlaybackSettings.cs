using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Audio;
using T3.Core.Resource;

namespace T3.Core.Operator
{
    /// <summary>
    /// Defines playback related settings for a symbol like its primary soundtrack or audio input device,
    /// BPM rate and other settings.
    /// </summary>
    public class PlaybackSettings
    {
        public bool Enabled { get; set; }
        public float Bpm { get; set; }
        public List<AudioClip> AudioClips { get; private set; } = new();
        public SyncModes SyncMode;

        public bool GetMainSoundtrack(out AudioClip soundtrack)
        {
            foreach (var clip in AudioClips)
            {
                if (!clip.IsSoundtrack)
                    continue;

                soundtrack = clip;
                return true;
            }

            soundtrack = null;
            return false;
        }

        public enum SyncModes
        {
            ProjectSoundTrack,
            ExternalSource,
        }

        public static void WriteToJson(JsonTextWriter writer, PlaybackSettings playbackSettings)
        {
            if (playbackSettings == null)
                return;

            var hasSettingsForClips = playbackSettings.Enabled || playbackSettings.AudioClips.Count > 0;
            if (!hasSettingsForClips)
                return;

            writer.WritePropertyName(nameof(PlaybackSettings));

            writer.WriteStartObject();
            {
                //writer.WriteEndArray();

                writer.WriteValue(nameof(PlaybackSettings.Enabled), playbackSettings.Enabled);

                // Write audio clips
                var audioClips = playbackSettings.AudioClips;
                if (audioClips != null && audioClips.Count != 0)
                {
                    writer.WritePropertyName("AudioClips");
                    writer.WriteStartArray();
                    foreach (var audioClip in audioClips)
                    {
                        audioClip.ToJson(writer);
                    }
                    writer.WriteEndArray();
                }
            }

            writer.WriteEndObject();
        }

        public static PlaybackSettings ReadFromJson(JToken o)
        {
            var clips = GetClips(o).ToList(); // Support legacy json format

            var settingsToken = (JObject)o[nameof(Symbol.PlaybackSettings)];
            if (settingsToken == null && clips.Count == 0)
                return null;

            var newSettings = new PlaybackSettings
                                  {
                                      AudioClips = clips
                                  };
            
            if (settingsToken != null)
            {
                newSettings.Enabled = SymbolJson.ReadBoolean(settingsToken, nameof(PlaybackSettings.Enabled));
                newSettings.Bpm = SymbolJson.ReadFloat(settingsToken, nameof(PlaybackSettings.Bpm));
                newSettings.SyncMode = SymbolJson.ReadEnum<PlaybackSettings.SyncModes>(settingsToken, nameof(PlaybackSettings.SyncMode));
                newSettings.AudioClips.AddRange(GetClips(settingsToken)); // Support correct format
            }
            
            if (newSettings.Bpm == 0 && newSettings.GetMainSoundtrack(out var soundtrack))
            {
                newSettings.Bpm = soundtrack.Bpm;
                newSettings.Enabled = true;
            }

            return newSettings;
        }

        private static IEnumerable<AudioClip> GetClips(JToken o)
        {
            var jAudioClipArray = (JArray)o[nameof(Symbol.PlaybackSettings.AudioClips)];
            if (jAudioClipArray != null)
            {
                foreach (var c in jAudioClipArray)
                {
                    yield return AudioClip.FromJson(c);
                }
            }
        }
    }
}