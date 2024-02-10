using System;
using System.Diagnostics;
using T3.Core.Operator;

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
    ///  - FxTime keeps running if "Idle Motion" (aka. "continued playback") is activated. The effect time should be used for most Operators.
    ///  - Time is used for all UI interactions and everything that is driven by keyframes.
    /// 
    /// RunTime is the time since application.
    /// IsLive is true if we are playing live, false if we are rendering
    /// </summary>
    public class Playback
    {
        public Playback()
        {
            _isLive = true;
            Current = this;
        }

        public static Playback Current { get; set; }
        public PlaybackSettings Settings { get; set; }

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
        public double TimeInSecs { get => TimeInBars * 240.0 / Bpm;
            set => TimeInBars = value * Bpm / 240.0; }

        public TimeRange LoopRange;

        public double Bpm { get;  set; } = 120.0;
        public bool IsLive
        {
            get => _isLive;
            set {
                _isLive = value;
                if (value)
                {
                    PlaybackSpeed = 0;
                    _lastFrameStart = RunTimeInSecs;
                }
                else
                {
                    _lastFrameStart = TimeInSecs;
                }
            }
        }
        
        public double PlaybackSpeed { get; set; }
        public bool IsLooping = false;
        public static bool OpNotReady;
        
        public static double RunTimeInSecs => _runTimeWatch.Elapsed.TotalSeconds;
        public static double LastFrameDuration { get; private set; }
        public double LastFrameDurationInBars => BarsFromSeconds(LastFrameDuration);
        
        public virtual void Update(bool idleMotionEnabled = false)
        {
            // if we are not live, TimeInBars is provided externally
            Current = this;
            var currentRuntime = (IsLive) ? RunTimeInSecs : TimeInSecs;

            LastFrameDuration = currentRuntime - _lastFrameStart;
            _lastFrameStart = currentRuntime;

            var timeSinceLastFrameInSecs = LastFrameDuration;
            var isPlaying = Math.Abs(PlaybackSpeed) > 0.001;

            if (!IsLive)
            {
                FxTimeInBars = TimeInBars;
            }
            else if (isPlaying)
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

            // don't support looping if recording (looping sound is not implemented yet)
            if (IsLive && IsLooping && TimeInBars > LoopRange.End)
            {
                double loopDuration = LoopRange.End - LoopRange.Start;

                // Jump to start if loop is negative or sound is too far out of time region
                if (loopDuration <= 0 || TimeInBars - LoopRange.End > 1.0)
                    TimeInBars = LoopRange.Start;
                else
                    TimeInBars -= loopDuration;
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
        private bool _isLive;
    }
}
