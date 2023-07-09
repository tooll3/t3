using System;
using System.Collections.Generic;
using System.IO;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Utils;
using T3.Editor.Gui.Styling;
using T3.Editor.SystemUi;
using T3.SystemUi;

namespace T3.Editor.Gui.Windows.RenderExport;

public abstract class RenderHelperWindow : Window
{
    private enum TimeReference
    {
        Bars,
        Seconds,
        Frames
    }

    protected static void DrawTimeSetup()
    {
        FormInputs.ResetIndent();
        // convert times if reference time selection changed
        var oldTimeReference = _timeReference;
            
        if (FormInputs.AddEnumDropdown(ref _timeReference, "Time reference"))
        {
            _startTime = (float)ConvertReferenceTime(_startTime, oldTimeReference, _timeReference);
            _endTime = (float)ConvertReferenceTime(_endTime, oldTimeReference, _timeReference);
        }

        // change FPS if required
        FormInputs.AddFloat("FPS", ref Fps, 0);
        if (Fps < 0) Fps = -Fps;
        if (Fps != 0)
        {
            _startTime = (float)ConvertFps(_startTime, _lastValidFps, Fps);
            _endTime = (float)ConvertFps(_endTime, _lastValidFps, Fps);
            _lastValidFps = Fps;
        }
        FormInputs.AddFloat($"Start in {_timeReference}", ref _startTime);
        FormInputs.AddFloat($"End in {_timeReference}", ref _endTime);
            
        // use our loop range instead of entered values?
        FormInputs.AddCheckBox("Use Loop Range", ref _useLoopRange);
        if (_useLoopRange) UseLoopRange();
            
        var startTimeInSeconds = ReferenceTimeToSeconds(_startTime, _timeReference);
        var endTimeInSeconds = ReferenceTimeToSeconds(_endTime, _timeReference);
        FrameCount = (int)Math.Round((endTimeInSeconds - startTimeInSeconds) * Fps);
            
        if (FormInputs.AddInt($"Motion Blur Samples", ref _overrideMotionBlurSamples, -1, 50, 1, "This requires a [RenderWithMotionBlur] operator. Please check its documentation."))
        {
            _overrideMotionBlurSamples = _overrideMotionBlurSamples.Clamp(-1, 50);
        }            
    }

    protected static bool ValidateOrCreateTargetFolder(string targetFile)
    {
        var directory = Path.GetDirectoryName(targetFile);
        if (targetFile != directory && File.Exists(targetFile))
        {
            // FIXME: get a nicer popup window here...
            var result = EditorUi.Instance.ShowMessageBox("File exists. Overwrite?", "Render Video", PopUpButtons.YesNo);
            return (result == PopUpResult.Yes);
        }

        if (directory == null || Directory.Exists(directory))
            return true;
        
        try
        {
            Directory.CreateDirectory(directory);
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to create target folder '{directory}': {e.Message}");
            return false;
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

    private static double ConvertFps(double time, double oldFps, double newFps)
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
                if (Fps != 0)
                    return time / Fps;
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
                if (Fps != 0)
                    return timeInSeconds * Fps;
                else
                    return timeInSeconds * 60.0;
        }

        // this is an error, don't change the value
        return timeInSeconds;
    }

    protected static void SetPlaybackTimeForNextFrame()
    {
        var startTimeInSeconds = ReferenceTimeToSeconds(_startTime, _timeReference);
        var endTimeInSeconds = ReferenceTimeToSeconds(_endTime, _timeReference);
        Playback.Current.TimeInSecs = MathUtils.Lerp(startTimeInSeconds, endTimeInSeconds, Progress);
    }

    public override List<Window> GetInstances()
    {
        return new List<Window>();
    }

    protected static float Progress => (float)(FrameIndex / (double)FrameCount).Clamp(0, 1);

    private static bool _useLoopRange;
    private static TimeReference _timeReference;
    private static float _startTime;
    private static float _endTime = 1.0f; // one Bar
    protected static float Fps = 60.0f;
    private static float _lastValidFps = Fps;

    public static bool IsExporting => _isExporting;
    // ReSharper disable once InconsistentNaming
    protected static bool _isExporting;
    public static int OverrideMotionBlurSamples => _overrideMotionBlurSamples;
    private static int _overrideMotionBlurSamples = -1;
        
    protected static int FrameIndex;
    protected static int FrameCount;
}