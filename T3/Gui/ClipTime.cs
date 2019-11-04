using ImGuiNET;
using ManagedBass;
using T3.Core.Operator;

namespace T3.Gui
{
    public abstract class ClipTime
    {
        public virtual double Time { get; set; }
        public double TimeRangeStart { get; set; } = 0;
        public double TimeRangeEnd { get; set; } = 8;
        public double Bpm { get; set; } = 120;
        public virtual double PlaybackSpeed { get; set; } = 0;
        public bool IsLooping = false;
        public TimeModes TimeMode { get; set; } = TimeModes.Seconds;

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
            EvaluationContext.GlobalTime = Time;
        }

        public enum TimeModes
        {
            Seconds,
            Bars,
            F30,
            F60,
        }

        protected abstract void UpdateTime();
    }

    public class UiClipTime : ClipTime
    {
        protected override void UpdateTime()
        {
            Time += ImGui.GetIO().DeltaTime * PlaybackSpeed;
        }
    }
    
    public class StreamClipTime : ClipTime
    {
        private readonly int _soundStreamHandle;
        
        public StreamClipTime(string filename)
        {
            Bass.Init();
            _soundStreamHandle = Bass.CreateStream(filename);
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
            get
            {
                var playbackState = Bass.ChannelIsActive(_soundStreamHandle);
                return playbackState == PlaybackState.Playing ? 1.0 : 0.0;
            }

            set
            {
                if (value == 0.0)
                {
                    Bass.ChannelStop(_soundStreamHandle);
                }
                else
                {
                    var playbackState = Bass.ChannelIsActive(_soundStreamHandle);
                    if (playbackState != PlaybackState.Playing)
                    {
                        Bass.ChannelPlay(_soundStreamHandle);
                    }
                }
            }
        }
        
        protected override void UpdateTime()
        {
            // nothing to do here as time is taken directly from stream
        }

        private double GetCurrentStreamTime()
        {
            long soundStreamPos = Bass.ChannelGetPosition(_soundStreamHandle);
            return Bass.ChannelBytes2Seconds(_soundStreamHandle, soundStreamPos);
        }
    }
}
