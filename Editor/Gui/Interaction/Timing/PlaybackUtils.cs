using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.Output;
using T3.Operators.Types.Id_79db48d8_38d3_47ca_9c9b_85dde2fa660d;
using T3.Operators.Types.Id_f5158500_39e4_481e_aa4f_f7dbe8cbe0fa;

namespace T3.Editor.Gui.Interaction.Timing
{
    public static class PlaybackUtils
    {
        public static void UpdatePlaybackAndSyncing()
        {
            var settings = FindPlaybackSettings();

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

                    //BeatTiming.SlideSyncTime = ForwardBeatTaps.SlideSyncTime;
                    Playback.Current.Settings.Bpm = (float)Playback.Current.Bpm;
                    
                    if (SetBpm.TryGetNewBpmRate(out var newBpmRate2))
                    {
                        Log.Warning("SetBpm in BeatTapping mode has no effect.");
                        // settings.Bpm = newBpmRate2;
                    }
                    
                    BeatTiming.Update();
                }                
            }
            else
            {
                Playback.Current = T3Ui.DefaultTimelinePlayback;
            }
            
            // Process callback from [SetBpm] operator
            if (SetBpm.TryGetNewBpmRate(out var newBpmRate))
            {
                settings.Bpm = newBpmRate;
            }

            Playback.Current.Bpm = settings.Bpm;
            Playback.Current.Update(UserSettings.Config.EnableIdleMotion);
            Playback.Current.Settings = settings;
        }

        private static PlaybackSettings FindPlaybackSettings()
        {
            var primaryGraphWindow = GraphWindow.GetPrimaryGraphWindow();
            var composition = primaryGraphWindow?.GraphCanvas.CompositionOp;

            if (FindPlaybackSettingsForInstance(composition, out _, out var settings))
                return settings;
            
            var outputWindow = OutputWindow.GetPrimaryOutputWindow();
            var pinnedOutput = outputWindow?.Pinning.GetPinnedOrSelectedInstance();
            if(FindPlaybackSettingsForInstance(pinnedOutput, out _, out var settingsFromPinned))
                return settingsFromPinned;
                
            return _defaultPlaybackSettings;
        }

        /// <summary>
        /// Scans the current composition path and its parents for a soundtrack 
        /// </summary>
        public static bool TryFindingSoundtrack(out AudioClip soundtrack)
        {
            var settings = FindPlaybackSettings();
            if (settings != null)
                return settings.GetMainSoundtrack(out soundtrack);
            
            soundtrack = null;
            return false;
        }

        /// <summary>
        /// Try to find playback settings for an instance.
        /// </summary>
        /// <returns>false if falling back to default settings</returns>
        public static bool FindPlaybackSettingsForInstance(Instance startInstance, out Instance instanceWithSettings, out PlaybackSettings settings)
        {
            instanceWithSettings = startInstance;
            while (true)
            {
                if (instanceWithSettings == null)
                {
                    settings = _defaultPlaybackSettings;
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

        private static readonly PlaybackSettings _defaultPlaybackSettings = new()
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