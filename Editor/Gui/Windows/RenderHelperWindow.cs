using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using ManagedBass;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.Logging;
using T3.Core.Utils;
using T3.Editor.Gui.Audio;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Styling;

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

        protected static void DrawTimeSetup()
        {
            // convert times if reference time selection changed
            var oldTimeReference = _timeReference;
            
            if (FormInputs.AddEnumDropdown(ref _timeReference, "Time reference"))
            {
                _startTime = (float)ConvertReferenceTime(_startTime, oldTimeReference, _timeReference);
                _endTime = (float)ConvertReferenceTime(_endTime, oldTimeReference, _timeReference);
            }

            // change FPS if required
            FormInputs.AddFloat("FPS", ref _fps, 0);
            if (_fps < 0) _fps = -_fps;
            if (_fps != 0)
            {
                _startTime = (float)ConvertFPS(_startTime, _lastValidFps, _fps);
                _endTime = (float)ConvertFPS(_endTime, _lastValidFps, _fps);
                _lastValidFps = _fps;
            }
            FormInputs.AddFloat($"Start in {_timeReference}", ref _startTime);
            FormInputs.AddFloat($"End in {_timeReference}", ref _endTime);
            
            // use our loop range instead of entered values?
            FormInputs.AddCheckBox("Use Loop Range", ref _useLoopRange);
            if (_useLoopRange) UseLoopRange();
            
            double startTimeInSeconds = ReferenceTimeToSeconds(_startTime, _timeReference);
            double endTimeInSeconds = ReferenceTimeToSeconds(_endTime, _timeReference);
            _frameCount = (int)Math.Round((endTimeInSeconds - startTimeInSeconds) * _fps);
            
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

        private static void UseLoopRange()
        {
            var playback = Playback.Current; // TODO, this should be non-static eventually
            var startInSeconds = playback.SecondsFromBars(playback.LoopRange.Start);
            var endInSeconds = playback.SecondsFromBars(playback.LoopRange.End);
            _startTime = (float)SecondsToReferenceTime(startInSeconds, _timeReference);
            _endTime = (float)SecondsToReferenceTime(endInSeconds, _timeReference);
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

        protected static void SetPlaybackTimeForNextFrame()
        {
            double startTimeInSeconds = ReferenceTimeToSeconds(_startTime, _timeReference);
            double endTimeInSeconds = ReferenceTimeToSeconds(_endTime, _timeReference);
            var oldTimeInSecs = Playback.Current.TimeInSecs;
            Playback.Current.TimeInSecs = MathUtils.Lerp(startTimeInSeconds, endTimeInSeconds, Progress);
            var fixedDeltaTime = Math.Max(Playback.Current.TimeInSecs - oldTimeInSecs, 0.0);
            var adaptedDeltaTime = Math.Max(Playback.Current.TimeInSecs - oldTimeInSecs + _timingOverhang, 0.0);

            //PlaybackUtils.UpdatePlaybackAndSyncing();
            var primaryGraphWindow = GraphWindow.GetPrimaryGraphWindow();
            var composition = primaryGraphWindow?.GraphCanvas.CompositionOp;
            PlaybackUtils.FindPlaybackSettings(composition, out var compWithSettings, out var settings);
            settings.GetMainSoundtrack(out var soundtrack);
            AudioEngine.UseAudioClip(soundtrack, Playback.Current.TimeInSecs, Playback.Current.IsLive == true);

            if (!_bassChanged)
            {
                // save the old live playback values
                _bassUpdatePeriod = Bass.GetConfig(Configuration.UpdatePeriod);
                _bassPlaybackBufferLength = Bass.GetConfig(Configuration.PlaybackBufferLength);
                _bassGlobalStreamVolume = Bass.GetConfig(Configuration.GlobalStreamVolume);
                // turn off automatic sound generation
                Bass.Configure(Configuration.UpdatePeriod, 0);
                Bass.Configure(Configuration.GlobalStreamVolume, 0);
                //Bass.Configure(Configuration.NetPreBuffer, 0);
                _bassChanged = true;

                Playback.Current.IsLive = false;
                _timingOverhang = 0.0;
            }

            Playback.Current.Bpm = settings.Bpm;
            Playback.Current.Update(false);
            Playback.Current.Settings = settings;

            if (adaptedDeltaTime > 0)
            {
                var bufferLengthInMS = (int)Math.Floor(1000.0 * adaptedDeltaTime);
                _timingOverhang = adaptedDeltaTime - (double)bufferLengthInMS / 1000.0;
                _timingOverhang = Math.Max(_timingOverhang, 0.0);
                Bass.Configure(Configuration.PlaybackBufferLength, bufferLengthInMS);

                AudioEngine.CompleteFrame(Playback.Current, (double)bufferLengthInMS / 1000.0);
            }
            else
            {
                // Do not advance audio on the initial time setting.
                // We may still be off since video is normally two frames behind...
                AudioEngine.CompleteFrame(Playback.Current, 0.0);
            }
        }

        protected static void ReleasePlaybackTime()
        {
            // Bass.Configure(Configuration.UpdateThreads, true);
            Playback.Current.TimeInSecs = ReferenceTimeToSeconds(_endTime, _timeReference);
            Playback.Current.IsLive = true;

            // restore live playback values
            if (_bassChanged)
            {
                Bass.Configure(Configuration.UpdatePeriod, _bassUpdatePeriod);
                Bass.Configure(Configuration.PlaybackBufferLength, _bassPlaybackBufferLength);
                Bass.Configure(Configuration.GlobalStreamVolume, _bassGlobalStreamVolume);
                _bassChanged = false;
            }
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        protected static float Progress => (float)((double)_frameIndex / (double)_frameCount).Clamp(0, 1);

        private static bool _useLoopRange;
        private static TimeReference _timeReference;
        private static float _startTime;
        private static float _endTime = 1.0f; // one Bar
        protected static float _fps = 60.0f;
        private static float _lastValidFps = _fps;

        private static double _timingOverhang; // time that could not be updated due to MS resolution (in seconds)

        private static bool _bassChanged = false; // were Bass library settings changed?
        private static int _bassUpdatePeriod; // initial Bass library update period in MS
        private static int _bassPlaybackBufferLength; // initial Bass library playback buffer length in MS
        private static int _bassGlobalStreamVolume; // initial Bass library sample volume (range 0 to 10000)

        public static bool IsExporting => _isExporting;
        public static int OverrideMotionBlurSamples => _overrideMotionBlurSamples;
        private static int _overrideMotionBlurSamples = -1;
        
        protected static bool _isExporting;
        protected static int _frameIndex;
        protected static int _frameCount;
    }
}