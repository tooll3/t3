using T3.Core.Animation;

namespace T3.Editor.Gui.Interaction.Timing
{
    /// <summary>
    /// Override the default Playback to support continuous playback synchronized to BPM
    /// </summary>
    public class BeatTimingPlayback : Playback
    {
        public override void Update(bool idleMotionEnabled = false)
        {
            FxTimeInBars = BeatTiming.BeatTime;
            Bpm = BeatTiming.Bpm;
            TimeInBars = FxTimeInBars;            
            
            // TODO: setting the context time here is kind of awkward
            //GlobalTimeForKeyframes = TimeInBars;
            // context.Playback.BeatTime = BeatTime;
            // context.Playback.Bpm = Bpm;
            // context.Playback.TimeInSecs = TimeInSecs;
        }
    }
}