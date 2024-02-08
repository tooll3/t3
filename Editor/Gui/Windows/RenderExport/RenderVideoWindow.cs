using System;
using System.IO;
using System.Text.RegularExpressions;
using ImGuiNET;
using SharpDX.Direct3D11;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Core.Utils;
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
        FormInputs.AddVerticalSpace(15);
        DrawTimeSetup();
        ImGui.Indent(5);
        DrawInnerContent();
    }

    private void DrawInnerContent()
    {
        var mainTexture = OutputWindow.GetPrimaryOutputWindow()?.GetCurrentTexture();

        if (FindIssueWithTexture(mainTexture, MfVideoWriter.SupportedFormats, out var warning))
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
            FormInputs.AddHint($"{q.Title} quality ({_bitrate * duration / 1024 / 1024 / 8:0} MB for {duration / 60:0}:{duration % 60:00}s at {size.Width}×{size.Height})");
            CustomComponents.TooltipForLastItem(q.Description);
        }

        FormInputs.AddStringInput("File", ref UserSettings.Config.RenderVideoFilePath);
        ImGui.SameLine();
        FileOperations.DrawFileSelector(FileOperations.FilePickerTypes.File, ref UserSettings.Config.RenderVideoFilePath);

        if (IsFilenameIncrementible())
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, _autoIncrementVersionNumber ? 0.7f : 0.3f);
            FormInputs.AddCheckBox("Increment version after export", ref _autoIncrementVersionNumber);
            ImGui.PopStyleVar();
        }

        FormInputs.AddCheckBox("Export Audio (experimental)", ref _exportAudio);

        ImGui.Separator();

        if (!_isExporting)
        {
            if (ImGui.Button("Start Export"))
            {
                if (ValidateOrCreateTargetFolder(UserSettings.Config.RenderVideoFilePath))
                {
                    _isExporting = true;
                    _exportStartedTime = Playback.RunTimeInSecs;
                    FrameIndex = 0;
                    SetPlaybackTimeForThisFrame();

                    if (_videoWriter == null)
                    {
                        _videoWriter = new Mp4VideoWriter(UserSettings.Config.RenderVideoFilePath, size, _exportAudio);
                        _videoWriter.Bitrate = _bitrate;

                        // FIXME: Allow floating point FPS in a future version
                        _videoWriter.Framerate = (int)Fps;
                    }

                    TextureReadAccess.ClearQueue();
                }
            }
        }
        else
        {
            var audioFrame = AudioEngine.LastMixDownBuffer(1.0 / Fps);
            var success = SaveCurrentFrameAndAdvance(ref mainTexture, ref audioFrame,
                                                     soundtrackChannels(), soundtrackSampleRate());
            ImGui.ProgressBar((float)Progress, new Vector2(-1, 4));

            var currentTime = Playback.RunTimeInSecs;
            var durationSoFar = currentTime - _exportStartedTime;

            if (GetRealFrame() >= FrameCount || !success)
            {
                var successful = success ? "successfully" : "unsuccessfully";
                _lastHelpString = $"Sequence export finished {successful} in {HumanReadableDurationFromSeconds(durationSoFar)}";
                _isExporting = false;
                TryIncrementingFileName();
            }
            else if (ImGui.Button("Cancel"))
            {
                _lastHelpString = $"Sequence export cancelled after {HumanReadableDurationFromSeconds(durationSoFar)}";
                _isExporting = false;
            }
            else
            {
                var estimatedTimeLeft = durationSoFar / Progress - durationSoFar;
                _lastHelpString = $"Saved {_videoWriter.FilePath} frame {GetRealFrame()}/{FrameCount}  ";
                _lastHelpString += $"{Progress * 100.0:0}%%  {HumanReadableDurationFromSeconds(estimatedTimeLeft)} left";
            }

            if (!_isExporting)
            {
                _videoWriter?.Dispose();
                _videoWriter = null;
                ReleasePlaybackTime();
            }
        }

        CustomComponents.HelpText(_lastHelpString);
    }

    private static readonly Regex _matchFileVersionPattern = new Regex(@"\bv(\d{2,4})\b");
    
    private static bool IsFilenameIncrementible()
    {
        var filename = Path.GetFileName(UserSettings.Config.RenderVideoFilePath);
        if (string.IsNullOrEmpty(filename))
            return false;
        
        var result = _matchFileVersionPattern.Match(filename);
        return result.Success;
    }

    private static void TryIncrementingFileName()
    {
        if (!_autoIncrementVersionNumber)
            return;
        
        var filename = Path.GetFileName(UserSettings.Config.RenderVideoFilePath);
        if (string.IsNullOrEmpty(filename))
            return;
        
        var result = _matchFileVersionPattern.Match(filename);
        if (!result.Success)
            return;
        
        var versionString = result.Groups[1].Value;
        if (!int.TryParse(versionString, out var versionNumber))
            return;
        
        var digits = versionString.Length.Clamp(2,4);
        var newVersionString = "v"+ (versionNumber +1).ToString("D"+digits);
        var newFilename = filename.Replace("v" + versionString, newVersionString);

        var directoryName = Path.GetDirectoryName(UserSettings.Config.RenderVideoFilePath);
        UserSettings.Config.RenderVideoFilePath = directoryName == null 
                                                      ? newFilename 
                                                      : Path.Combine(directoryName, newFilename);
    }

    private string HumanReadableDurationFromSeconds(double seconds)
    {
        return $"{(int)(seconds / 60 / 60):00}:{(seconds / 60)%60:00}:{seconds%60:00}";
    }

    private static int GetRealFrame()
    {
        // since we are double-buffering and discarding the first few frames,
        // we have to subtract these frames to get the currently really shown framenumber...
        return FrameIndex - MfVideoWriter.SkipImages;
    }

    private static bool SaveCurrentFrameAndAdvance(ref Texture2D mainTexture, ref byte[] audioFrame,
                                                   int channels, int sampleRate)
    {
        if (Playback.OpNotReady)
        {
            Log.Debug("Waiting for operators to complete");
            return true;
        }
        try
        {
            var savedFrame = _videoWriter.ProcessFrames(ref mainTexture, ref audioFrame, channels, sampleRate);
            if (!savedFrame)
            {
                Log.Debug($"Skipping {FrameIndex}");
            }
            FrameIndex++;
            SetPlaybackTimeForThisFrame();
        }
        catch (Exception e)
        {
            _lastHelpString = e.ToString();
            _isExporting = false;
            _videoWriter?.Dispose();
            _videoWriter = null;
            ReleasePlaybackTime();
            return false;
        }

        return true;
    }

    private QualityLevel GetQualityLevelFromRate(float bitsPerPixelSecond)
    {
        QualityLevel q = default;
        for (var index = _qualityLevels.Length - 1; index >= 0; index--)
        {
            q = _qualityLevels[index];
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
        
        public readonly double MinBitsPerPixelSecond;
        public readonly string Title;
        public readonly string Description;
    }

    private readonly QualityLevel[] _qualityLevels = new[]
                                                        {
                                                            new QualityLevel(0.01, "Poor", "Very low quality. Consider lower resolution."),
                                                            new QualityLevel(0.02, "Low", "Probable strong artifacts"),
                                                            new QualityLevel(0.05, "Medium", "Will exhibit artifacts in noisy regions"),
                                                            new QualityLevel(0.08, "Okay", "Compromise between filesize and quality"),
                                                            new QualityLevel(0.12, "Good", "Good quality. Probably sufficient for YouTube."),
                                                            new QualityLevel(0.5, "Very good", "Excellent quality, but large."),
                                                            new QualityLevel(1, "Reference", "Indistinguishable. Very large files."),
                                                        };
    
    private static int _bitrate = 25000000;

    private static bool _autoIncrementVersionNumber = true;
    private static bool _exportAudio = true;
    private static Mp4VideoWriter _videoWriter;
    private static string _lastHelpString = string.Empty;
}