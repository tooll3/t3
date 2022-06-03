using System;
using ManagedBass;
using T3.Core.Logging;
using T3.Core.Operator;

//using ImGuiNET;

namespace T3.Core.Animation
{
    public class TimeFormat
    {
        private static int GetBeatTimeBar(double timeInBars, int startCountFrom)
        {
            return (int)(timeInBars) + startCountFrom;   // NOTE:  We count bars from Zero because it matches the current time
        }

        private static int GetBeatTimeBeat(double timeInBars, int startCountFrom)
        {
            return (int)(timeInBars * 4) % 4 + startCountFrom;
        }

        private static int GetBeatTimeTick(double timeInBars, int startCountFrom)
        {
            return (int)(timeInBars * 16) % 4 + startCountFrom;
        }

        public static string FormatTimeInBars(double timeInBars, int startCountFrom)
        {
            return $"{GetBeatTimeBar(timeInBars, startCountFrom):0}.{GetBeatTimeBeat(timeInBars, startCountFrom):0}.{GetBeatTimeTick(timeInBars,startCountFrom):0}.";
        }

        private static double BarsToSeconds(double timeInBars, double bpm)
        {
            return timeInBars * 240 / bpm;
        }

        public enum TimeDisplayModes
        {
            Secs,
            Bars,
            F30,
            F60,
        }
    }

    public class Playback : IDisposable
    {
        public static Playback Current { get; private set; }
        
        /// <summary>
        /// The absolute current time as controlled by the timeline interaction.
        /// </summary>
        public virtual double TimeInBars { get; set; }
        
        /// <summary>
        /// Convenience function to convert from internal TimeInBars mapped to seconds for current BPM. 
        /// </summary>
        public double TimeInSecs { get => TimeInBars * 240 / Bpm; set => TimeInBars = value / Bpm * 240f; }

        /// <summary>
        /// The current time used for animation (would advance from <see cref="TimeInBars"/> if keepBeatTimeRunning is active. 
        /// </summary>
        public double BeatTime { get; set; }
        public TimeRange LoopRange;
        
        public double Bpm = 120;
        public double SoundtrackOffsetInSecs = 0;
        public double SoundtrackOffsetInBars => SoundtrackOffsetInSecs * Bpm / 240;
        
        public virtual double PlaybackSpeed { get; set; } = 0;
        public bool IsLooping = false;

        public virtual void Update(float timeSinceLastFrameInSecs, bool keepBeatTimeRunning = false)
        {
            UpdateTime(timeSinceLastFrameInSecs, keepBeatTimeRunning);
            if (IsLooping && TimeInBars > LoopRange.End)
            {
                TimeInBars = TimeInBars - LoopRange.End > 1.0 // Jump to start if too far out of time region
                                 ? LoopRange.Start
                                 : TimeInBars - (LoopRange.End - LoopRange.Start);
            }

            
            // FIXME: With multiple graphs, this breaks frame duration  
            EvaluationContext.GlobalTimeForKeyframes = TimeInBars;
            var frameDurationInBars = BeatTime - EvaluationContext.GlobalTimeForEffects; 
            EvaluationContext.GlobalTimeForEffects = BeatTime;
            EvaluationContext.BPM = Bpm;
            EvaluationContext.LastFrameDuration = timeSinceLastFrameInSecs;
            EvaluationContext.GlobalTimeInSecs = TimeInSecs;
            Current = this;
        }


        protected virtual void UpdateTime(float timeSinceLastFrameInSecs, bool keepBeatTimeRunning)
        {
            var isPlaying = Math.Abs(PlaybackSpeed) > 0.001;

            if (isPlaying)
            {
                TimeInBars += timeSinceLastFrameInSecs * PlaybackSpeed * Bpm / 240f + SoundtrackOffsetInSecs;
                BeatTime = TimeInBars;
            }
            else if (keepBeatTimeRunning)
            {
                BeatTime += timeSinceLastFrameInSecs * Bpm / 240f + SoundtrackOffsetInSecs;
            }
        }

        public virtual float GetSongDurationInSecs()
        {
            return 120; // fallback
        }

        public virtual void Dispose()
        {
        }
        
                

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

                BeatTime = value;
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

        protected override void UpdateTime(float timeSinceLastFrameInSecs, bool keepBeatTimeRunning)
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
                BeatTime = TimeInBars;
            }
            else if (keepBeatTimeRunning)
            {
                BeatTime += timeSinceLastFrameInSecs * Bpm / 240f;
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
