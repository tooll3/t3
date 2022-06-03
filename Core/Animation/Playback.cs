using System;
using System.Diagnostics;
using ManagedBass;
using T3.Core.Logging;

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
    public class Playback : IDisposable
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
        public double TimeInSecs { get => TimeInBars * 240 / Bpm; set => TimeInBars = value / Bpm * 240f; }

        public TimeRange LoopRange;
        
        public double Bpm = 120;
        public double SoundtrackOffsetInSecs = 0;
        public double SoundtrackOffsetInBars => SoundtrackOffsetInSecs * Bpm / 240;
        
        public virtual double PlaybackSpeed { get; set; } = 0;
        public bool IsLooping = false;
        
        public static double RunTimeInSecs => _runTimeWatch.ElapsedMilliseconds / 1000f;
        public static double LastFrameDuration { get; private set; }
        private static double _lastFrameStart;
        
        public virtual void Update(float timeSinceLastFrameInSecs, bool idleMotionEnabled = false)
        {
            var currentRuntime = RunTimeInSecs;
            LastFrameDuration = currentRuntime - _lastFrameStart;
            _lastFrameStart = currentRuntime;
            
            UpdateTime(timeSinceLastFrameInSecs, idleMotionEnabled);
            if (IsLooping && TimeInBars > LoopRange.End)
            {
                TimeInBars = TimeInBars - LoopRange.End > 1.0 // Jump to start if too far out of time region
                                 ? LoopRange.Start
                                 : TimeInBars - (LoopRange.End - LoopRange.Start);
            }
        }

        
        private double ToSecs(double timeInBars)
        {
            return timeInBars * 240 / Bpm;
        }


        protected virtual void UpdateTime(float timeSinceLastFrameInSecs, bool idleMotionEnabled)
        {
            var isPlaying = Math.Abs(PlaybackSpeed) > 0.001;

            if (isPlaying)
            {
                TimeInBars += timeSinceLastFrameInSecs * PlaybackSpeed * Bpm / 240f;
                FxTimeInBars = TimeInBars;
            }
            else if (idleMotionEnabled)
            {
                FxTimeInBars += timeSinceLastFrameInSecs * Bpm / 240f;
            }
        }

        public virtual float GetSongDurationInSecs()
        {
            return 120; // fallback
        }

        public virtual void Dispose()
        {
        }
        
        private static readonly Stopwatch _runTimeWatch = Stopwatch.StartNew();

    }

    public class StreamPlayback : Playback
    {
        private int _soundStreamHandle;
        private float _defaultPlaybackFrequency;

        public StreamPlayback(string filepath)
        {
            LoadFile(filepath);
        }

        public void LoadFile(string filepath)
        {
            Bass.Free();
            Bass.Init();
            _soundStreamHandle = Bass.CreateStream(filepath, 0,0, BassFlags.Prescan);
            Bass.ChannelGetAttribute(_soundStreamHandle, ChannelAttribute.Frequency, out _defaultPlaybackFrequency);
        }

        public override double TimeInBars
        {
            get => (GetCurrentStreamTime() +  SoundtrackOffsetInSecs) * Bpm / 240f;
            set
            {
                _timeInSeconds = value * 240f / Bpm - SoundtrackOffsetInSecs;
                if (IsTimeWithinAudioTrack)
                {
                    SetStreamPositionFromTime();
                }

                FxTimeInBars = value;
            }
        }

        private void SetStreamPositionFromTime()
        {
            long soundStreamPos = Bass.ChannelSeconds2Bytes(_soundStreamHandle, _timeInSeconds);
            Bass.ChannelSetPosition(_soundStreamHandle, soundStreamPos);
        }

        private static bool _printedInitErrorOnce = false;

        public override float GetSongDurationInSecs()
        {
            var length = Bass.ChannelGetLength(_soundStreamHandle);
            if (length < 0 && !_printedInitErrorOnce)
            {
                Log.Error("Failed to initialize audio playback. Possible reasons are: A: Your PC doesn't have a sound card installed. B: You headphone are speaker are not plugged in.");
                _printedInitErrorOnce = true;
            }
            return (float)Bass.ChannelBytes2Seconds(_soundStreamHandle, length);
        }

        public long StreamLength => Bass.ChannelGetLength(_soundStreamHandle);
        public long StreamPos => Bass.ChannelGetPosition(_soundStreamHandle);

        public override double PlaybackSpeed
        {
            get => _playbackSpeed;
            set
            {
                _playbackSpeed = value;
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (value == 0.0)
                {
                    Bass.ChannelStop(_soundStreamHandle);
                }
                else if (value < 0.0)
                {
                    Bass.ChannelSetAttribute(_soundStreamHandle, ChannelAttribute.ReverseDirection, -1);
                    Bass.ChannelSetAttribute(_soundStreamHandle, ChannelAttribute.Frequency, _defaultPlaybackFrequency * -_playbackSpeed);
                    Bass.ChannelPlay(_soundStreamHandle);
                }
                else
                {
                    Bass.ChannelSetAttribute(_soundStreamHandle, ChannelAttribute.ReverseDirection, 1);
                    Bass.ChannelSetAttribute(_soundStreamHandle, ChannelAttribute.Frequency, _defaultPlaybackFrequency * _playbackSpeed);
                    Bass.ChannelPlay(_soundStreamHandle);
                }
            }
        }

        public void SetMuteMode(bool shouldBeMuted)
        {
            Bass.ChannelGetAttribute(_soundStreamHandle, ChannelAttribute.Volume, out float actualVolume);
            if (actualVolume > 0.0f)
            {
                _previousVolume = actualVolume;
            }

            Bass.ChannelSetAttribute(_soundStreamHandle, ChannelAttribute.Volume, shouldBeMuted ? 0 : _previousVolume);
        }

        protected override void UpdateTime(float timeSinceLastFrameInSecs, bool idleMotionEnabled)
        {
            if (_playbackSpeed < 0.0 || !IsTimeWithinAudioTrack)
            {
                // bass can't play backwards, so do it manually
                TimeInBars += timeSinceLastFrameInSecs * _playbackSpeed * Bpm / 240f;
                Bass.ChannelPause(_soundStreamHandle);
            }
            else if (Bass.ChannelIsActive(_soundStreamHandle) == PlaybackState.Paused)
            {
                Bass.ChannelPlay(_soundStreamHandle);
                SetStreamPositionFromTime();
            }

            var isPlaying = Math.Abs(_playbackSpeed) > 0.001;
            if (isPlaying)
            {
                FxTimeInBars = TimeInBars;
            }
            else if (idleMotionEnabled)
            {
                FxTimeInBars += timeSinceLastFrameInSecs * Bpm / 240f;
            }
        }

        private double GetCurrentStreamTime()
        {
            if (!IsTimeWithinAudioTrack)
            {
                return _timeInSeconds;
            }

            long soundStreamPos = Bass.ChannelGetPosition(_soundStreamHandle);
            
            if (_playbackSpeed > 0)
            {
                UpdateFftBuffer();
            } 
            return Bass.ChannelBytes2Seconds(_soundStreamHandle, soundStreamPos);
        }

        public override void Dispose()
        {
            Bass.Free();
        }
        private const int FftSize = 256;
        public static readonly float[] FftBuffer =  new float[FftSize];

        private void UpdateFftBuffer()
        {
            const int get256FftValues = (int)DataFlags.FFT512;
            Bass.ChannelGetData(_soundStreamHandle, FftBuffer, get256FftValues);
        }


        private bool IsTimeWithinAudioTrack => _timeInSeconds >= 0 && _timeInSeconds < GetSongDurationInSecs();
        private double _timeInSeconds; // We use this outside of stream range

        private double _playbackSpeed;
        private float _previousVolume;
    }
}
