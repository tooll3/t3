using T3.Editor.Gui.Graph;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.Operator;

namespace T3.Editor.Gui.Audio
{
    public static class SoundtrackUtils
    {
        public static void UpdateMainSoundtrack()
        {
            var primaryGraphWindow = GraphWindow.GetPrimaryGraphWindow();
            var composition = primaryGraphWindow?.GraphCanvas.CompositionOp;

            if (!FindParentWithPlaybackSettings(composition, out var compWithSettings, out var settings))
                return;
                
            Playback.Current.Bpm = settings.Bpm;
            if (settings.GetMainSoundtrack(out var soundtrack))
            {
                AudioEngine.UseAudioClip(soundtrack, Playback.Current.TimeInSecs);                
            }
        }

        /// <summary>
        /// Scans the current composition path and its parents for a soundtrack 
        /// </summary>
        public static bool TryFindingSoundtrack(Instance composition, out AudioClip soundtrack)
        {
            if (FindParentWithPlaybackSettings(composition, out var compositionWithSettings, out var settings))
            {
                return settings.GetMainSoundtrack(out soundtrack);
            }

            soundtrack = null;
            return false;
        }

        public static bool FindParentWithPlaybackSettings(Instance startInstance, out Instance instanceWithSettings, out PlaybackSettings settings)
        {
            instanceWithSettings = startInstance;
            while (true)
            {
                if (instanceWithSettings == null)
                {
                    settings = null;
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
    }
}