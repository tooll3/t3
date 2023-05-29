using System;
using System.Collections.Generic;
using System.IO;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.Logging;
using T3.Core.Utils;
using T3.Editor.Gui.Audio;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Styling;
using T3.Editor.SystemUi;
using T3.SystemUi;

namespace T3.Editor.Gui.Windows
{
    public abstract class RenderHelperWindow : Window
    {
        public enum TimeReference
        {
            Bars,
            Seconds,
            Frames
        }

        protected static int soundtrackChannels()
        {
            var primaryGraphWindow = GraphWindow.GetPrimaryGraphWindow();
            var composition = primaryGraphWindow?.GraphCanvas.CompositionOp;
            PlaybackUtils.FindPlaybackSettings(composition, out var compWithSettings, out var settings);
            settings.GetMainSoundtrack(out var soundtrack);
            return AudioEngine.clipChannels(soundtrack);
        }

        protected static int soundtrackSampleRate()
        {
            var primaryGraphWindow = GraphWindow.GetPrimaryGraphWindow();
            var composition = primaryGraphWindow?.GraphCanvas.CompositionOp;
            PlaybackUtils.FindPlaybackSettings(composition, out var compWithSettings, out var settings);
            settings.GetMainSoundtrack(out var soundtrack);
            return AudioEngine.clipSampleRate(soundtrack);
        }

        protected static void DrawTimeSetup()
        {
            // convert times if reference time selection changed
            var oldTimeReference = _timeReference;
            
            if (FormInputs.AddEnumDropdown(ref _timeReference, "Time reference"))
            {
                _startTime = ConvertReferenceTime(_startTime, oldTimeReference, _timeReference);
                _endTime = ConvertReferenceTime(_endTime, oldTimeReference, _timeReference);
            }

            // convert times to float for GUI
            var fpsFloat = (float)_fps;
            var startTimeFloat = (float)_startTime;
            var endTimeFloat = (float)_endTime;

            FormInputs.AddFloat("FPS", ref fpsFloat, 0);
            FormInputs.AddFloat($"Start in {_timeReference}", ref startTimeFloat);
            FormInputs.AddFloat($"End in {_timeReference}", ref endTimeFloat);

            // convert back to double
            _fps = (double)fpsFloat;
            _startTime = (double)startTimeFloat;
            _endTime = (double)endTimeFloat;

            // change FPS if required
            if (_fps < 0) _fps = -_fps;
            if (_fps != 0)
            {
                _startTime = ConvertFPS(_startTime, _lastValidFps, _fps);
                _endTime = ConvertFPS(_endTime, _lastValidFps, _fps);
                _lastValidFps = _fps;
            }
            
            // use our loop range instead of entered values?
            FormInputs.AddCheckBox("Use Loop Range", ref _useLoopRange);
            if (_useLoopRange) UseLoopRange();
            
            double startTimeInSeconds = ReferenceTimeToSeconds(_startTime, _timeReference);
            double requestedEndTimeInSeconds = ReferenceTimeToSeconds(_endTime, _timeReference);
            _frameCount = (int)Math.Ceiling((requestedEndTimeInSeconds - startTimeInSeconds) * _fps);

            if (FormInputs.AddInt($"Motion Blur Samples", ref _overrideMotionBlurSamples, -1, 50, 1, "This requires a [RenderWithMotionBlur] operator. Please check its documentation."))
            {
                _overrideMotionBlurSamples = _overrideMotionBlurSamples.Clamp(-1, 50);
            }            
        }

        protected static bool ValidateOrCreateTargetFolder(string targetFile)
        {
            string directory = Path.GetDirectoryName(targetFile);
            if (targetFile != directory && File.Exists(targetFile))
            {
                // FIXME: get a nicer popup window here...
                var result = EditorUi.Instance.ShowMessageBox("File exists. Overwrite?", "Render Video", PopUpButtons.YesNo);
                return (result == PopUpResult.Yes);
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

        private static void UseLoopRange()
        {
            var playback = Playback.Current; // TODO, this should be non-static eventually
            var startInSeconds = playback.SecondsFromBars(playback.LoopRange.Start);
            var endInSeconds = playback.SecondsFromBars(playback.LoopRange.End);
            _startTime = SecondsToReferenceTime(startInSeconds, _timeReference);
            _endTime = SecondsToReferenceTime(endInSeconds, _timeReference);
        }

        private static double ConvertReferenceTime(double time,
                                                   TimeReference oldTimeReference,
                                                   TimeReference newTimeReference)
        {
            // only convert time value if time reference changed
            if (oldTimeReference == newTimeReference) return time;

            var seconds = ReferenceTimeToSeconds(time, oldTimeReference);
            return SecondsToReferenceTime(seconds, newTimeReference);
        }

        private static double ConvertFPS(double time, double oldFps, double newFps)
        {
            // only convert FPS if values are valid
            if (oldFps == 0 || newFps == 0) return time;

            return time / oldFps * newFps;
        }

        private static double ReferenceTimeToSeconds(double time, TimeReference timeReference)
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

        private static double SecondsToReferenceTime(double timeInSeconds, TimeReference timeReference)
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

        protected static void SetPlaybackTimeForThisFrame()
        {
            if (Progress <= 0.0)
                _timingOverhang = 0.0;

            // get playback settings
            var primaryGraphWindow = GraphWindow.GetPrimaryGraphWindow();
            var composition = primaryGraphWindow?.GraphCanvas.CompositionOp;
            PlaybackUtils.FindPlaybackSettings(composition, out var compWithSettings, out var settings);

            // change settings for all playback before calculating times
            Playback.Current.Bpm = settings.Bpm;
            Playback.Current.Settings = settings;

            // set user time in secs for video playback
            double startTimeInSeconds = ReferenceTimeToSeconds(_startTime, _timeReference);
            double endTimeInSeconds = startTimeInSeconds + (_frameCount-1) / _fps;
            var oldTimeInSecs = Playback.Current.TimeInSecs;
            Playback.Current.TimeInSecs = MathUtils.Lerp(startTimeInSeconds, endTimeInSeconds, Progress);
            var adaptedDeltaTime = Math.Max(Playback.Current.TimeInSecs - oldTimeInSecs + _timingOverhang, 0.0);

            // set user time in secs for audio playback
            settings.GetMainSoundtrack(out var soundtrack);
            if (soundtrack != null)
                AudioEngine.UseAudioClip(soundtrack, Playback.Current.TimeInSecs);

            if (!_recording)
            {
                _timingOverhang = 0.0;
                adaptedDeltaTime = 1.0 / _fps;

                Playback.Current.IsLive = false;
                Playback.Current.PlaybackSpeed = 1.0;

                AudioEngine.prepareRecording(Playback.Current, _fps);

                double requestedEndTimeInSeconds = ReferenceTimeToSeconds(_endTime, _timeReference);
                double actualEndTimeInSeconds = startTimeInSeconds + _frameCount / _fps;

                Log.Debug($"Requested recording from {startTimeInSeconds:0.0000} to {requestedEndTimeInSeconds:0.0000} seconds");
                Log.Debug($"Actually recording from {startTimeInSeconds:0.0000} to {actualEndTimeInSeconds:0.0000} seconds due to frame raster");
                Log.Debug($"Using {Playback.Current.Bpm} bpm");

                _recording = true;
            }

            // update audio parameters, respecting looping etc.
            Playback.Current.Update(false);

            var bufferLengthInMS = (int)Math.Floor(1000.0 * adaptedDeltaTime);
            _timingOverhang = adaptedDeltaTime - (double)bufferLengthInMS / 1000.0;
            _timingOverhang = Math.Max(_timingOverhang, 0.0);

            AudioEngine.CompleteFrame(Playback.Current, (double)bufferLengthInMS / 1000.0);
        }

        protected static void ReleasePlaybackTime()
        {
            AudioEngine.endRecording(Playback.Current, _fps);

            Playback.Current.TimeInSecs = ReferenceTimeToSeconds(_endTime, _timeReference);
            Playback.Current.IsLive = true;
            Playback.Current.PlaybackSpeed = 0.0;
            Playback.Current.Update(false);

            _recording = false;
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        protected static double Progress => (_frameCount <= 1) ? 0 :
            ((double)_frameIndex / (double)(_frameCount-1)).Clamp(0, 1);

        private static bool _useLoopRange;
        private static TimeReference _timeReference;
        private static double _startTime;
        private static double _endTime = 1.0f; // one Bar
        protected static double _fps = 60.0f;
        private static double _lastValidFps = _fps;

        private static double _timingOverhang; // time that could not be updated due to MS resolution (in seconds)
        private static bool _recording = false; // are we recording?

        public static bool IsExporting => _isExporting;
        public static int OverrideMotionBlurSamples => _overrideMotionBlurSamples;
        private static int _overrideMotionBlurSamples = -1;
        
        protected static bool _isExporting;
        protected static int _frameIndex;
        protected static int _frameCount;
    }
}