using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using ImGuiNET;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Logging;

namespace T3.Gui.Windows
{
    public abstract class RenderHelperWindow : Window
    {
        public enum TimeReference
        {
            Bars,
            Seconds,
            Frames
        }

        protected void DrawTimeSetup()
        {
            // use our loop range instead of entered values?
            ImGui.Checkbox("Use Loop Range", ref _useLoopRange);
            if (_useLoopRange) UseLoopRange();

            // convert times if reference time selection changed
            int newTimeReferenceIndex = (int)_timeReference;
            if (CustomComponents.DrawEnumSelector<TimeReference>(ref newTimeReferenceIndex, "Time reference"))
            {
                TimeReference newTimeReference = (TimeReference)newTimeReferenceIndex;
                _startTime = (float)ConvertReferenceTime(_startTime, _timeReference, newTimeReference);
                _endTime = (float)ConvertReferenceTime(_endTime, _timeReference, newTimeReference);
                _timeReference = newTimeReference;
            }

            CustomComponents.FloatValueEdit($"Start in {_timeReference}", ref _startTime);
            CustomComponents.FloatValueEdit($"End in {_timeReference}", ref _endTime);

            // change FPS if required
            CustomComponents.FloatValueEdit("FPS", ref _fps, 0);
            if (_fps < 0) _fps = -_fps;
            if (_fps != 0)
            {
                _startTime = (float)ConvertFPS(_startTime, _lastValidFps, _fps);
                _endTime = (float)ConvertFPS(_endTime, _lastValidFps, _fps);
                _lastValidFps = _fps;
            }

            double startTimeInSeconds = ReferenceTimeToSeconds(_startTime, _timeReference);
            double endTimeInSeconds = ReferenceTimeToSeconds(_endTime, _timeReference);
            _frameCount = (int)Math.Round((endTimeInSeconds - startTimeInSeconds) * _fps);
        }

        protected static bool ValidateOrCreateTargetFolder(string targetFile)
        {
            string directory = Path.GetDirectoryName(targetFile);
            if (targetFile != directory && File.Exists(targetFile))
            {
                const MessageBoxButtons buttons = MessageBoxButtons.YesNo;

                // FIXME: get a nicer popup window here...
                var result = MessageBox.Show("File exists. Overwrite?", "Render Video", buttons);
                return (result == DialogResult.Yes);
            }

            if (!Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception e)
                {
                    Log.Warning($"Failed to create target folder '{directory}': {e.Message}");
                    return false;
                }
            }
            return true;
        }

        public static void UseLoopRange()
        {
            var playback = Playback.Current; // TODO, this should be non-static eventually
            var startInSeconds = playback.SecondsFromBars(playback.LoopRange.Start);
            var endInSeconds = playback.SecondsFromBars(playback.LoopRange.End);
            _startTime = (float)SecondsToReferenceTime(startInSeconds, _timeReference);
            _endTime = (float)SecondsToReferenceTime(endInSeconds, _timeReference);
        }

        public static double ConvertReferenceTime(double time,
            TimeReference oldTimeReference,
            TimeReference newTimeReference)
        {
            // only convert time value if time reference changed
            if (oldTimeReference == newTimeReference) return time;

            var seconds = ReferenceTimeToSeconds(time, oldTimeReference);
            return SecondsToReferenceTime(seconds, newTimeReference);
        }

        protected static double ConvertFPS(double time, double oldFps, double newFps)
        {
            // only convert FPS if values are valid
            if (oldFps == 0 || newFps == 0) return time;

            return time / oldFps * newFps;
        }

        protected static double ReferenceTimeToSeconds(double time, TimeReference timeReference)
        {
            var playback = Playback.Current; // TODO, this should be non-static eventually
            switch (timeReference)
            {
                case TimeReference.Bars:
                    return playback.SecondsFromBars(time);
                case TimeReference.Seconds:
                    return time;
                case TimeReference.Frames:
                    if (_fps != 0)
                        return time / _fps;
                    else
                        return time / 60.0;
            }

            // this is an error, don't change the value
            return time;
        }

        protected static double SecondsToReferenceTime(double timeInSeconds, TimeReference timeReference)
        {
            var playback = Playback.Current; // TODO, this should be non-static eventually
            switch (timeReference)
            {
                case TimeReference.Bars:
                    return playback.BarsFromSeconds(timeInSeconds);
                case TimeReference.Seconds:
                    return timeInSeconds;
                case TimeReference.Frames:
                    if (_fps != 0)
                        return timeInSeconds * _fps;
                    else
                        return timeInSeconds * 60.0;
            }

            // this is an error, don't change the value
            return timeInSeconds;
        }

        protected static void SetPlaybackTimeForNextFrame()
        {
            double startTimeInSeconds = ReferenceTimeToSeconds(_startTime, _timeReference);
            double endTimeInSeconds = ReferenceTimeToSeconds(_endTime, _timeReference);
            Playback.Current.TimeInSecs = MathUtils.Lerp(startTimeInSeconds, endTimeInSeconds, Progress);
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        protected static float Progress => (float)((double)_frameIndex / (double)_frameCount).Clamp(0, 1);

        protected static bool _useLoopRange;
        protected static TimeReference _timeReference;
        protected static float _startTime;
        protected static float _endTime = 1.0f; // one Bar
        protected static float _fps = 60.0f;
        protected static float _lastValidFps = _fps;

        protected static int _frameIndex;
        protected static int _frameCount;
    }
}