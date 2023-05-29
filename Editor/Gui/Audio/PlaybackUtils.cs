using T3.Editor.Gui.Graph;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Gui.Interaction.Timing;
using T3.Editor.Gui.UiHelpers;
using T3.Operators.Types.Id_79db48d8_38d3_47ca_9c9b_85dde2fa660d;
using T3.Operators.Types.Id_f5158500_39e4_481e_aa4f_f7dbe8cbe0fa;

namespace T3.Editor.Gui.Audio
{
    public static class PlaybackUtils
    {
        public static void UpdatePlaybackAndSyncing()
        {
            var primaryGraphWindow = GraphWindow.GetPrimaryGraphWindow();
            var composition = primaryGraphWindow?.GraphCanvas.CompositionOp;

            FindPlaybackSettings(composition, out var compWithSettings, out var settings);

            WasapiAudioInput.StartFrame(settings);
            
            if (settings.AudioSource == PlaybackSettings.AudioSources.ProjectSoundTrack)
            {
                if (settings.GetMainSoundtrack(out var soundtrack))
                {
                    AudioEngine.UseAudioClip(soundtrack, Playback.Current.TimeInSecs);
                }
            }

            if (settings.AudioSource == PlaybackSettings.AudioSources.ExternalDevice
                && settings.Syncing == PlaybackSettings.SyncModes.Tapping)
            {
                Playback.Current = T3Ui.DefaultBeatTimingPlayback;
                
                if (Playback.Current.Settings is { Syncing: PlaybackSettings.SyncModes.Tapping })
                {
                    if (ForwardBeatTaps.BeatTapTriggered)
                        BeatTiming.TriggerSyncTap();

                    if (ForwardBeatTaps.ResyncTriggered)
                        BeatTiming.TriggerResyncMeasure();

                    BeatTiming.SlideSyncTime = ForwardBeatTaps.SlideSyncTime;
                    Playback.Current.Settings.Bpm = (float)Playback.Current.Bpm;
                    
                    if (SetBpm.TryGetNewBpmRate(out var newBpmRate2))
                    {
                        Log.Warning("SetBpm in BeatTapping mode has effect.");
                        settings.Bpm = newBpmRate2;
                    }
                    
                    BeatTiming.Update();
                }                
            }
            else
            {
                Playback.Current = T3Ui.DefaultTimelinePlayback;
            }
            
            if (SetBpm.TryGetNewBpmRate(out var newBpmRate))
            {
                settings.Bpm = newBpmRate;
            }

            Playback.Current.Bpm = settings.Bpm;
            Playback.Current.Update(UserSettings.Config.EnableIdleMotion);
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