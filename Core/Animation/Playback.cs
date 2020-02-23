using System;
using ManagedBass;
using T3.Core.Operator;

//using ImGuiNET;

namespace T3.Core.Animation
{
    public class Playback
    {
        
        public virtual double TimeInBars { get; set; }
        public virtual double TimeInSecs
        {
            get => TimeInBars * 240 / Bpm;
            set => TimeInBars = value / Bpm * 240f;
        }

        public virtual double BeatTime { get; set; }
        public double TimeRangeStart { get; set; } = 0;
        public double TimeRangeEnd { get; set; } = 8;
        public double Bpm = 120;  
        public virtual double PlaybackSpeed { get; set; } = 0;
        public bool IsLooping = false;
        public TimeModes TimeMode { get; set; } = TimeModes.Bars;
        
        public int Bar => (int)(TimeInBars) + 1;
        public int Beat => (int)(TimeInBars * 4) % 4 + 1;
        public int Tick => (int)(TimeInBars * 16) % 4 + 1;

        public void Update(float timeSinceLastFrameInSecs)
        {
            UpdateTime(timeSinceLastFrameInSecs);
            if (IsLooping && TimeInBars > TimeRangeEnd)
            {
                TimeInBars = TimeInBars - TimeRangeEnd > 1.0 // Jump to start if too far out of time region
                                 ? TimeRangeStart
                                 : TimeInBars - (TimeRangeEnd - TimeRangeStart);
            }

            // TODO: setting the context time here is kind of awkward
            EvaluationContext.GlobalTimeInBars = TimeInBars;
            EvaluationContext.BeatTime = BeatTime;
        }

        public enum TimeModes
        {
            Secs,
            Bars,
            F30,
            F60,
        }

        protected virtual void UpdateTime(float timeSinceLastFrameInSecs)
        {
            //var deltaTime = ImGui.GetIO().DeltaTime;
            var isPlaying = Math.Abs(PlaybackSpeed) > 0.001;

            if (isPlaying)
            {
                TimeInBars += timeSinceLastFrameInSecs * PlaybackSpeed * Bpm / 240f;
                //BeatTime = TimeInBars * Bpm / 60.0 / 4.0;
                BeatTime = TimeInBars;
            }
            else
            {
                //BeatTime += timeSinceLastFrameInSecs * Bpm / 60.0 / 4.0;
                BeatTime += timeSinceLastFrameInSecs * Bpm / 240f;
            }
        }

        public virtual float GetSongDurationInSecs()
        {
            return 120;
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
                var timeInSecs = value * 240f / Bpm;
                long soundStreamPos = Bass.ChannelSeconds2Bytes(_soundStreamHandle, timeInSecs);
                Bass.ChannelSetPosition(_soundStreamHandle, soundStreamPos);
            }
        }

        public override float GetSongDurationInSecs()
        {
            var length = Bass.ChannelGetLength(_soundStreamHandle);
            return (float)Bass.ChannelBytes2Seconds(_soundStreamHandle, length);
        }

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
            if (Bass.Volume > 0)
            {
                _previousVolume = Bass.Volume;
            }

            Bass.Volume = shouldBeMuted ? 0 : _previousVolume;
        }

        protected override void UpdateTime(float timeSinceLastFrameInSecs)
        {
            //var deltaTime = ImGui.GetIO().DeltaTime;
            if (_playbackSpeed < 0.0)
            {
                // bass can't play backwards, so do it manually
                TimeInBars += timeSinceLastFrameInSecs * _playbackSpeed * Bpm / 240f;
            }

            var isPlaying = Math.Abs(_playbackSpeed) > 0.001;
            if (isPlaying)
            {
                BeatTime = TimeInBars; // * Bpm / 60.0 / 4.0;
            }
            else
            {
                BeatTime += timeSinceLastFrameInSecs * Bpm / 240f;
            }
        }

        private double GetCurrentStreamTime()
        {
            long soundStreamPos = Bass.ChannelGetPosition(_soundStreamHandle);
            return Bass.ChannelBytes2Seconds(_soundStreamHandle, soundStreamPos);
        }

        private double _playbackSpeed;
        private double _previousVolume;
    }
}