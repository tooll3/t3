using System;
using ImGuiNET;
using SharpDX.Direct3D11;
using T3.Core.Animation;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.Output;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.Windows.RenderExport;

public class RenderVideoWindow : RenderHelperWindow
{
    public RenderVideoWindow()
    {
        Config.Title = "Render Video";
        _lastHelpString = RenderHelperWindow.PreferredInputFormatHint;
    }


    protected override void DrawContent()
    {
        DrawTimeSetup();

        var mainTexture = OutputWindow.GetPrimaryOutputWindow()?.GetCurrentTexture();
     
        if(FindIssueWithTexture(mainTexture, MfVideoWriter.SupportedFormats, out var warning))
        {
            CustomComponents.HelpText(warning);
            return;
        }

        Int2 size = default;
        var currentDesc = mainTexture!.Description;
        size.Width = currentDesc.Width;
        size.Height = currentDesc.Height;

        // Custom parameters for this renderer
        FormInputs.AddInt("Bitrate", ref _bitrate, 0, 25000000, 1000);
        {
            var duration = FrameCount / Fps;
            double bitsPerPixelSecond = _bitrate / (size.Width * size.Height * Fps);
            var q = GetQualityLevelFromRate((float)bitsPerPixelSecond);
            FormInputs.AddHint($"{q.Title} quality ({_bitrate * duration / 1024 / 1024 / 8:0} MB for {duration/60:0}:{duration%60:00}s at {size.Width}×{size.Height})");
            CustomComponents.TooltipForLastItem(q.Description);
        }
        
        FormInputs.AddStringInput("File", ref _targetFile);
        ImGui.SameLine();
        FileOperations.DrawFileSelector(FileOperations.FilePickerTypes.File, ref _targetFile);
        ImGui.Separator();


        if (!_isExporting)
        {
            if (ImGui.Button("Start Export"))
            {
                if (ValidateOrCreateTargetFolder(_targetFile))
                {
                    _previousPlaybackSpeed = Playback.Current.PlaybackSpeed;
                    Playback.Current.PlaybackSpeed = 1;
                    _isExporting = true;
                    _exportStartedTime = Playback.RunTimeInSecs;
                    FrameIndex = 0;
                    SetPlaybackTimeForNextFrame();

                    if (_videoWriter == null)
                    {
                        _videoWriter = new Mp4VideoWriter(_targetFile, size);
                        _videoWriter.Bitrate = _bitrate;

                        
                        // FIXME: Allow floating point FPS in a future version
                        _videoWriter.Framerate = (int)Fps;
                    }
                }
            }
        }
        else
        {
            var success = SaveCurrentFrameAndAdvance(ref mainTexture);
            ImGui.ProgressBar(Progress, new Vector2(-1, 4));

            var currentTime = Playback.RunTimeInSecs;
            var durationSoFar = currentTime - _exportStartedTime;
            if (GetRealFrame() >= FrameCount || !success)
            {
                var successful = success ? "successfully" : "unsuccessfully";
                _lastHelpString = $"Sequence export finished {successful} in {durationSoFar:0.00}s";
                _isExporting = false;
            }
            else if (ImGui.Button("Cancel"))
            {
                _lastHelpString = $"Sequence export cancelled after {durationSoFar:0.00}s";
                _isExporting = false;
            }
            else
            {
                var estimatedTimeLeft = durationSoFar /  Progress - durationSoFar;
                _lastHelpString = $"Saved {_videoWriter.FilePath} frame {FrameIndex}/{FrameCount}  ";
                _lastHelpString += $"{Progress * 100.0:0}%%  {HumanReadableDurationFromSeconds(estimatedTimeLeft)} left";
            }

            if (!_isExporting)
            {
                _videoWriter?.Dispose();
                _videoWriter = null;
                Playback.Current.PlaybackSpeed = _previousPlaybackSpeed;
            }
        }
        
        CustomComponents.HelpText(_lastHelpString);
    }

    private string HumanReadableDurationFromSeconds(double seconds)
    {
        if (seconds < 60)
        {
            return $"{seconds:0.0}s";
        }

        if (seconds < 60 * 60)
        {
            return $"{(int)(seconds/60):0}:{(int)(seconds%60):00}s";
        }

        return $"{(int)(seconds / 60 / 60):0}h {seconds/60%60:0}:{seconds%60:00}s";
    }

    private static int GetRealFrame()
    {
        // since we are double-buffering and discarding the first few frames,
        // we have to subtract these frames to get the currently really shown framenumber...
        return FrameIndex - MfVideoWriter.SkipImages;
    }

    private static bool SaveCurrentFrameAndAdvance(ref Texture2D mainTexture)
    {
        if (Playback.OpNotReady)
        {
            Log.Debug("Waiting for operators to complete");
            return true;
        }
        try
        {
            _videoWriter.AddVideoFrame(ref mainTexture);
            FrameIndex++;
            SetPlaybackTimeForNextFrame();
        }
        catch (Exception e)
        {
            _lastHelpString = e.ToString();
            _isExporting = false;
            _videoWriter?.Dispose();
            _videoWriter = null;
            return false;
        }

        return true;
    }

    private QualityLevel GetQualityLevelFromRate(float bitsPerPixelSecond)
    {
        QualityLevel q = default;
        for (var index = QualityLevels.Length - 1; index >= 0; index--)
        {
            q = QualityLevels[index];
            if (q.MinBitsPerPixelSecond < bitsPerPixelSecond)
                break;
        }

        return q;

    }

    private static double _exportStartedTime;

    private struct QualityLevel
    {
        public QualityLevel(double bits, string title, string description)
        {
            MinBitsPerPixelSecond = bits;
            Title = title;
            Description = description;
        }
        
        public double MinBitsPerPixelSecond;
        public string Title;
        public string Description;
    }

    private QualityLevel[] QualityLevels = new[]
                                               {
                                                   new QualityLevel(0.01, "Poor", "Very low quality. Consider lower resolution."),
                                                   new QualityLevel(0.02, "Low", "Probable strong artifacts"),
                                                   new QualityLevel(0.05, "Medium", "Will exhibit artifacts in noisy regions"),
                                                   new QualityLevel(0.08, "Okay", "Compromise between filesize and quality"),
                                                   new QualityLevel(0.12, "Good", "Good quality. Probably sufficient for YouTube."),
                                                   new QualityLevel(0.5, "Very good", "Excellent quality, but large."),
                                                   new QualityLevel(1, "Reference", "Indistinguishable. Very large files."),
                                               };
    
    private static int _bitrate = 15000000;
    private static string _targetFile = "./Render/output.mp4";

    private static Mp4VideoWriter _videoWriter = null;
    private static string _lastHelpString = string.Empty;
    private double _previousPlaybackSpeed;
}