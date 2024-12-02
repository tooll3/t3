using System.Diagnostics.CodeAnalysis;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.IO;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.Output;

namespace T3.Editor.Gui.Interaction.Timing;

public static class PlaybackUtils
{
    public static IBpmProvider BpmProvider;
    public static ITapProvider TapProvider;

    private static readonly HashSet<AudioClipInfo> AudioClipInstances = new();
        
    public static void UpdatePlaybackAndSyncing()
    {
        var settings = FindPlaybackSettings(out var audioComposition);

        if (settings == null)
            return;

        WasapiAudioInput.StartFrame(settings);
            
        if (settings.AudioSource == PlaybackSettings.AudioSources.ProjectSoundTrack)
        {
            if (settings.GetMainSoundtrack(audioComposition, out var soundtrack))
            {
                AudioEngine.UseAudioClip(soundtrack.Value, Playback.Current.TimeInSecs);
            }
        }

        bool hasBpmProvider = BpmProvider != null;

        if (settings.AudioSource == PlaybackSettings.AudioSources.ExternalDevice
            && settings.Syncing == PlaybackSettings.SyncModes.Tapping)
        {
            Playback.Current = T3Ui.DefaultBeatTimingPlayback;
                
            if (Playback.Current.Settings is { Syncing: PlaybackSettings.SyncModes.Tapping })
            {
                if (TapProvider != null)
                {
                    if(TapProvider.BeatTapTriggered)
                        BeatTiming.TriggerSyncTap();
                        
                    if (TapProvider.ResyncTriggered)
                        BeatTiming.TriggerResyncMeasure();
                        
                }
                    
                //BeatTiming.SlideSyncTime = ForwardBeatTaps.SlideSyncTime;
                Playback.Current.Settings.Bpm = (float)Playback.Current.Bpm;
                    
                if (hasBpmProvider && BpmProvider.TryGetNewBpmRate(out var newBpmRate2))
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
        if (hasBpmProvider && BpmProvider.TryGetNewBpmRate(out var newBpmRate))
        {
            settings.Bpm = newBpmRate;
        }

        Playback.Current.Bpm = settings.Bpm;
        Playback.Current.Update(UserSettings.Config.EnableIdleMotion);
        Playback.Current.Settings = settings;
    }

    private static PlaybackSettings? FindPlaybackSettings(out IResourceConsumer? owner)
    {
        var primaryGraphWindow = GraphWindow.Focused;
        var composition = primaryGraphWindow?.CompositionOp;

        if (FindPlaybackSettingsForInstance(composition, out var instance, out var settings))
        {
            owner = instance;
            return settings;
        }
            
        owner = null;
        return null;
            
        // var outputWindow = OutputWindow.GetPrimaryOutputWindow();
        //
        // if (outputWindow == null)
        // {
        //     owner = null;
        //     return null;
        // }
        //
        // if (outputWindow.Pinning.TryGetPinnedOrSelectedInstance(out var pinnedOutput, out _))
        // {
        //     if (FindPlaybackSettingsForInstance(pinnedOutput, out var instanceWithSettings, out var settingsFromPinned))
        //     {
        //         owner = instanceWithSettings;
        //         return settingsFromPinned;
        //     }
        //
        //     owner = null;
        //     return GetDefaultPlaybackSettings(composition?.Symbol.SymbolPackage);
        // }
        //
        // owner = null;
        return null;
    }

    /// <summary>
    /// Scans the current composition path and its parents for a soundtrack 
    /// </summary>
    public static bool TryFindingSoundtrack([NotNullWhen(true)] out AudioClipInfo? soundtrack, out IResourceConsumer composition)
    {
        var settings = FindPlaybackSettings(out composition);
        if (settings != null)
            return settings.GetMainSoundtrack(composition, out soundtrack);
            
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
                settings = GetDefaultPlaybackSettings(startInstance?.Symbol.SymbolPackage);
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

    private static PlaybackSettings GetDefaultPlaybackSettings(SymbolPackage package) => new()
                                                                                             {
                                                                                                 Enabled = false,
                                                                                                 Bpm = 120,
                                                                                                 AudioSource = PlaybackSettings.AudioSources.ProjectSoundTrack,
                                                                                                 Syncing = PlaybackSettings.SyncModes.Timeline,
                                                                                                 AudioInputDeviceName = null,
                                                                                                 AudioGainFactor = 1,
                                                                                                 AudioDecayFactor = 1,
                                                                                             };
}