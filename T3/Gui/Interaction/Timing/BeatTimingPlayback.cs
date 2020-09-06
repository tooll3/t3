using System;
using T3.Core.Animation;
using T3.Core.Operator;

namespace T3.Gui.Interaction.Timing
{
    /// <summary>
    /// Override the default Playback to support continuous playback synchronized to BPM
    /// </summary>
    public class BeatTimingPlayback : Playback
    {
        public override void Update(float timeSinceLastFrameInSecs, bool keepBeatTimeRunning = false)
        {
            BeatTime = T3Ui.BeatTiming.GetSyncedBeatTiming();
            Bpm = T3Ui.BeatTiming.Bpm;
            TimeInBars = BeatTime;            
            
            // TODO: setting the context time here is kind of awkward
            EvaluationContext.GlobalTimeInBars = TimeInBars;
            EvaluationContext.BeatTime = BeatTime;
            EvaluationContext.GlobalTimeInSecs = TimeInSecs;
        }
    }
}