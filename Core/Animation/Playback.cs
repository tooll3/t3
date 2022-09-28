using System;
using System.Diagnostics;

namespace T3.Core.Animation
{
    /// <summary>
    /// Some notes terminology:  "Time" vs. "TimeInSecs" - Default measure for time (unless it has the "*InSecs" suffix) is a "bar".
    /// So at 120 BPM a unit of time is 2 seconds.    
    /// 
    /// "Local" vs. "Playback": Local time can be overridden for by operators (like [SetCommandTime]) and [TimeClip]. These should be
    /// used for most operators.  The global Playback time is provided the Playback used in the output. The global time should be used
    /// for updating operators to have consistent and predictable "timing" even in sub-graphs with overridden time.
    /// Examples for this are [Pulsate] or [Counter]. 
    /// 
    /// "Time" vs "FxTime"
    ///  - FxTime keeps running if "continued playback" is activated. The effect time should be used for most Operators.
    ///  - Time is used for all UI interactions and everything that is driven by keyframes.
    /// 
    /// RunTime is the time since application.
    /// </summary>
    public class Playback
    {
        public Playback()
        {
            Current = this;
        }
        
        public static Playback Current { get; private set; }
        
        /// <summary>
        /// The absolute current time as controlled by the timeline interaction in bars.
        /// </summary>
        public virtual double TimeInBars { get; set; }
        
        /// <summary>
        /// The current time used for animation (would advance from <see cref="TimeInBars"/> if Idle Motion is enabled. 
        /// </summary>
        public double FxTimeInBars { get; protected set; }
        
        /// <summary>
        /// Convenience function to convert from internal TimeInBars mapped to seconds for current BPM. 
        /// </summary>
        public double TimeInSecs { get => TimeInBars * 240 / Bpm; 
            set => TimeInBars = value * Bpm / 240f; }

        public TimeRange LoopRange;
        
        public double Bpm = 120;
        
        public double PlaybackSpeed { get; set; }
        public bool IsLooping = false;
        
        public static double RunTimeInSecs => _runTimeWatch.ElapsedMilliseconds / 1000.0;
        public static double LastFrameDuration { get; private set; }
        public double LastFrameDurationInBars => BarsFromSeconds(LastFrameDuration);
        
        public virtual void Update(bool idleMotionEnabled = false)
        {
            var currentRuntime = RunTimeInSecs;
            LastFrameDuration = currentRuntime - _lastFrameStart;
            _lastFrameStart = currentRuntime;

            var timeSinceLastFrameInSecs = LastFrameDuration;
            var isPlaying = Math.Abs(PlaybackSpeed) > 0.001;

            if (isPlaying)
            {
                TimeInBars += timeSinceLastFrameInSecs * PlaybackSpeed * Bpm / 240.0;
                FxTimeInBars = TimeInBars;
            }
            else
            {
                var timeWasManipulated = Math.Abs(TimeInBars - _previousTime) > 0.001f;
                if (timeWasManipulated)
                {
                    FxTimeInBars = TimeInBars;
                }
                else if (idleMotionEnabled)
                {
                    FxTimeInBars += timeSinceLastFrameInSecs * Bpm / 240f;
                }
            }

            if (IsLooping && TimeInBars > LoopRange.End)
            {
                TimeInBars = TimeInBars - LoopRange.End > 1.0 // Jump to start if too far out of time region
                                 ? LoopRange.Start
                                 : TimeInBars - (LoopRange.End - LoopRange.Start);
            }

            _previousTime = TimeInBars;
        }

        public double BarsFromSeconds(double secs)
        {
            return secs * Bpm / 240.0;
        }
        
        public double SecondsFromBars(double bars)
        {
            return bars * 240.0 / Bpm;
        }
        
        private static double _lastFrameStart;
        private double _previousTime;
        private static readonly Stopwatch _runTimeWatch = Stopwatch.StartNew();
    }
}
