using System.Collections.Generic;
using T3.Core.Audio;

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
        
        public AudioClip MainSoundtrack  {
            get
            {
                foreach (var c in AudioClips)
                {
                    if (c.IsSoundtrack)
                        return c;
                }
                return null;
            }
        }
        
        public enum SyncModes
        {
            ProjectSoundTrack,
            ExternalSource,
        }
    }
}