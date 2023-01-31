using T3.Editor.Gui.Graph;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.Operator;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Audio
{
    public static class PlaybackUtils
    {
        public static void UpdatePlaybackForCurrentComposition()
        {
            var primaryGraphWindow = GraphWindow.GetPrimaryGraphWindow();
            var composition = primaryGraphWindow?.GraphCanvas.CompositionOp;

            FindPlaybackSettings(composition, out var compWithSettings, out var settings);

            if (settings.AudioSource == PlaybackSettings.AudioSources.ExternalDevice)
            {
                WasapiAudioInput.StartFrame(settings);
            }
            else
            {
                if (settings.GetMainSoundtrack(out var soundtrack))
                {
                    AudioEngine.UseAudioClip(soundtrack, Playback.Current.TimeInSecs);                
                }                
            }
            
            Playback.Current.Update(UserSettings.Config.EnableIdleMotion);
            Playback.Current.Bpm = settings.Bpm;
            Playback.Current.Settings = settings;
        }

        /// <summary>
        /// Scans the current composition path and its parents for a soundtrack 
        /// </summary>
        public static bool TryFindingSoundtrack(Instance composition, out AudioClip soundtrack)
        {
            if (FindPlaybackSettings(composition, out var compositionWithSettings, out var settings))
            {
                return settings.GetMainSoundtrack(out soundtrack);
            }

            soundtrack = null;
            return false;
        }

        /// <summary>
        /// Try to find playback settings for an instance.
        /// </summary>
        /// <returns>false if falling back to default settings</returns>
        public static bool FindPlaybackSettings(Instance startInstance, out Instance instanceWithSettings, out PlaybackSettings settings)
        {
            instanceWithSettings = startInstance;
            while (true)
            {
                if (instanceWithSettings == null)
                {
                    settings = DefaultPlaybackSettings;
                    instanceWithSettings = null;
                    return false;
                }
                
                settings = instanceWithSettings.Symbol.PlaybackSettings;
                if (settings != null && settings.Enabled)
                {
                    return true;
                }
                
                instanceWithSettings = instanceWithSettings.Parent;
            }
        }

        private static readonly PlaybackSettings DefaultPlaybackSettings = new PlaybackSettings
                                                                               {
                                                                                   Enabled = false,
                                                                                   Bpm = 120,
                                                                                   AudioSource = PlaybackSettings.AudioSources.ProjectSoundTrack,
                                                                                   Syncing = PlaybackSettings.SyncModes.Timeline,
                                                                                   AudioInputDeviceName = null,
                                                                                   AudioGainFactor = 1,
                                                                                   AudioDecayFactor = 1
                                                                               };
    }
}