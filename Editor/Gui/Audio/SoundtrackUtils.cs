using System.Linq;
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

            if (composition != null && TryFindingSoundtrack(composition, out var soundtrack))
            {
                Playback.Current.Bpm = soundtrack.Bpm;
                AudioEngine.UseAudioClip(soundtrack, Playback.Current.TimeInSecs);
            } 
        }

        /// <summary>
        /// Scans the current composition path and it's parents for a soundtrack 
        /// </summary>
        public static bool TryFindingSoundtrack(Instance composition, out AudioClip soundtrack)
        {
            //soundtrackSymbol = null;
            while (true)
            {
                var soundtrackSymbol = composition.Symbol;
                soundtrack = soundtrackSymbol.SoundSettings.AudioClips.SingleOrDefault(ac => ac.IsSoundtrack);
                if (soundtrack != null)
                {
                    return true;
                }

                if (composition.Parent == null)
                {
                    //soundtrackSymbol = null;
                    //Log.Debug("no soundtrack found");
                    return false;
                }

                composition = composition.Parent;
            }
        }


    }
}