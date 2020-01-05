using System;
using ImGuiNET;
using ManagedBass;
using T3.Core.Operator;

namespace T3.Gui
{
    public class ClipTime
    {
        public virtual double Time { get; set; }
        public virtual double BeatTime { get; set; }
        public double TimeRangeStart { get; set; } = 0;
        public double TimeRangeEnd { get; set; } = 8;
        public double Bpm { get; set; } = 95.08f;
        public virtual double PlaybackSpeed { get; set; } = 0;
        public bool IsLooping = false;
        public TimeModes TimeMode { get; set; } = TimeModes.Bars;

        public int Bar => (int)(Time * Bpm / 60.0 / 4.0) + 1;
        public int Beat => (int)(Time * Bpm / 60.0) % 4 + 1;
        public int Tick => (int)(Time * Bpm / 60.0 * 4) % 4 + 1;

        public void Update()
        {
            UpdateTime();
            if (IsLooping && Time > TimeRangeEnd)
            {
                Time = Time - TimeRangeEnd > 1.0 // Jump to start if too far out of time region
                           ? TimeRangeStart
                           : Time - (TimeRangeEnd - TimeRangeStart);
            }

            // TODO: setting the context time here is kind of awkward
            EvaluationContext.GlobalTime = Time;
            EvaluationContext.BeatTime = BeatTime;
        }

        public enum TimeModes
        {
            Secs,
            Bars,
            F30,
            F60,
        }

        protected virtual void UpdateTime()
        {
            var deltaTime = ImGui.GetIO().DeltaTime;
            var isPlaying = Math.Abs(PlaybackSpeed) > 0.001;
            if (isPlaying)
            {
                Time += deltaTime * PlaybackSpeed;
                BeatTime = Time * Bpm / 60.0 / 4.0;    
            }
            else
            {
                BeatTime += deltaTime * Bpm / 60.0 / 4.0;
            }
        }
    }

    public class StreamClipTime : ClipTime
    {
        private readonly int _soundStreamHandle;
        private float _originalFrequency;

        public StreamClipTime(string filename)
        {
            Bass.Init();
            _soundStreamHandle = Bass.CreateStream(filename);
            Bass.ChannelGetAttribute(_soundStreamHandle, ChannelAttribute.Frequency, out _originalFrequency);
        }

        public override double Time
        {
            get => GetCurrentStreamTime();
            set
            {
                long soundStreamPos = Bass.ChannelSeconds2Bytes(_soundStreamHandle, value);
                Bass.ChannelSetPosition(_soundStreamHandle, soundStreamPos);
            }
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
                    Bass.ChannelSetAttribute(_soundStreamHandle, ChannelAttribute.Frequency, _originalFrequency * -_playbackSpeed);
                    Bass.ChannelPlay(_soundStreamHandle);
                }
                else
                {
                    Bass.ChannelSetAttribute(_soundStreamHandle, ChannelAttribute.ReverseDirection, 1);
                    Bass.ChannelSetAttribute(_soundStreamHandle, ChannelAttribute.Frequency, _originalFrequency * _playbackSpeed);
                    Bass.ChannelPlay(_soundStreamHandle);
                }
            }
        }

        public void SetMuteMode(bool isMuted)
        {
            Bass.Volume =  isMuted ? 0 :1;
        }

        protected override void UpdateTime()
        {
            var deltaTime = ImGui.GetIO().DeltaTime;
            if (_playbackSpeed < 0.0)
            {
                // bass can't play backwards, so do it manually
                Time += deltaTime * _playbackSpeed;
            }

            var isPlaying = Math.Abs(_playbackSpeed) > 0.001;
            if (isPlaying)
            {
                BeatTime = Time * Bpm / 60.0 / 4.0;
            }
            else 
            {
                BeatTime += deltaTime * Bpm / 60.0 / 4.0;
            }
        }

        private double GetCurrentStreamTime()
        {
            long soundStreamPos = Bass.ChannelGetPosition(_soundStreamHandle);
            return Bass.ChannelBytes2Seconds(_soundStreamHandle, soundStreamPos);
        }


        private double _playbackSpeed;
    }
}