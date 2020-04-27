using System;
using ManagedBass;
using T3.Core.Operator;

//using ImGuiNET;

namespace T3.Core.Animation
{
    public class Playback : IDisposable
    {
        public virtual double TimeInBars { get; set; }
        public double TimeInSecs { get => TimeInBars * 240 / Bpm; set => TimeInBars = value / Bpm * 240f; }

        public double BeatTime { get; set; }
        public TimeRange LoopRange;
        public double Bpm = 120;
        public virtual double PlaybackSpeed { get; set; } = 0;
        public bool IsLooping = false;
        public TimeDisplayModes TimeDisplayMode { get; set; } = TimeDisplayModes.Bars;

        public int Bar => (int)(TimeInBars) + 1;
        public int Beat => (int)(TimeInBars * 4) % 4 + 1;
        public int Tick => (int)(TimeInBars * 16) % 4 + 1;

        public virtual void Update(float timeSinceLastFrameInSecs, bool keepBeatTimeRunning = false)
        {
            UpdateTime(timeSinceLastFrameInSecs, keepBeatTimeRunning);
            if (IsLooping && TimeInBars > LoopRange.End)
            {
                TimeInBars = TimeInBars - LoopRange.End > 1.0 // Jump to start if too far out of time region
                                 ? LoopRange.Start
                                 : TimeInBars - (LoopRange.End - LoopRange.Start);
            }

            // TODO: setting the context time here is kind of awkward
            EvaluationContext.GlobalTimeInBars = TimeInBars;
            EvaluationContext.BeatTime = BeatTime;
            EvaluationContext.GlobalTimeInSecs = TimeInSecs;
        }

        public enum TimeDisplayModes
        {
            Secs,
            Bars,
            F30,
            F60,
        }

        protected virtual void UpdateTime(float timeSinceLastFrameInSecs, bool keepBeatTimeRunning)
        {
            var isPlaying = Math.Abs(PlaybackSpeed) > 0.001;

            if (isPlaying)
            {
                TimeInBars += timeSinceLastFrameInSecs * PlaybackSpeed * Bpm / 240f;
                BeatTime = TimeInBars;
            }
            else if (keepBeatTimeRunning)
            {
                BeatTime += timeSinceLastFrameInSecs * Bpm / 240f;
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
        private readonly int _soundStreamHandle;
        private readonly float _defaultPlaybackFrequency;

        public StreamPlayback(string filename)
        {
            Bass.Init();
            _soundStreamHandle = Bass.CreateStream(filename);
            Bass.ChannelGetAttribute(_soundStreamHandle, ChannelAttribute.Frequency, out _defaultPlaybackFrequency);
        }

        public override double TimeInBars
        {
            get => GetCurrentStreamTime() * Bpm / 240f;
            set
            {
                _timeInSeconds = value * 240f / Bpm;
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

        public override float GetSongDurationInSecs()
        {
            var length = Bass.ChannelGetLength(_soundStreamHandle);
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
            return Bass.ChannelBytes2Seconds(_soundStreamHandle, soundStreamPos);
        }

        public override void Dispose()
        {
            Bass.Free();
        }

        private bool IsTimeWithinAudioTrack => _timeInSeconds >= 0 && _timeInSeconds < GetSongDurationInSecs();
        private double _timeInSeconds; // We use this outside of stream range

        private double _playbackSpeed;
        private float _previousVolume;
    }
}