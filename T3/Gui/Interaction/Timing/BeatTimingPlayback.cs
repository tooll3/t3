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
            BeatTime = BeatTiming.BeatTime;
            Bpm = BeatTiming.Bpm;
            TimeInBars = BeatTime;            
            
            // TODO: setting the context time here is kind of awkward
            EvaluationContext.GlobalTimeForKeyframes = TimeInBars;
            EvaluationContext.GlobalTimeForEffects = BeatTime;
            EvaluationContext.BPM = Bpm;
            EvaluationContext.GlobalTimeInSecs = TimeInSecs;
        }
    }
}