using System;
using System.IO;
using ImGuiNET;
using SharpDX.Direct3D11;
using T3.Core.Animation;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.Output;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.Windows.RenderExport;

public class RenderSequenceWindow : RenderHelperWindow
{
    public RenderSequenceWindow()
    {
        Config.Title = "Render Sequence";
        _lastHelpString = PreferredInputFormatHint;
    }
    
    protected override void DrawContent()
    {
        DrawTimeSetup();

        // Custom parameters for this renderer
        FormInputs.AddEnumDropdown(ref _fileFormat, "FileFormat");
        FormInputs.AddStringInput("Folder", ref _targetFolder);
        ImGui.SameLine();
        FileOperations.DrawFileSelector(FileOperations.FilePickerTypes.Folder, ref _targetFolder);
        ImGui.Separator();

        var mainTexture = OutputWindow.GetPrimaryOutputWindow()?.GetCurrentTexture();
        if (FindIssueWithTexture(mainTexture, ScreenshotWriter.SupportedInputFormats, out var warning))
        {
            CustomComponents.HelpText(warning);
            return;
            
        }

        if (!IsExporting)
        {
            if (ImGui.Button("Start Export"))
            {
                if (ValidateOrCreateTargetFolder(_targetFolder))
                {
                    _previousPlaybackSpeed = Playback.Current.PlaybackSpeed;
                    Playback.Current.PlaybackSpeed = 1;
                    _isExporting = true;
                    _exportStartedTime = Playback.RunTimeInSecs;
                    FrameIndex = 0;
                    SetPlaybackTimeForNextFrame();
                }
            }
        }
        else
        {
            var success = SaveCurrentFrameAndAdvance(mainTexture);
            ScreenshotWriter.UpdateSaving();
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
                var estimatedTimeLeft = durationSoFar - durationSoFar /  Progress;
                _lastHelpString = $"Saved {ScreenshotWriter.LastFilename} frame {GetRealFrame()+1}/{FrameCount}  ";
                _lastHelpString += $"{Progress * 100.0:0}%  {estimatedTimeLeft:0}s left";
            }

            if (!IsExporting &&  ScreenshotWriter.SavingComplete)
            {
                ScreenshotWriter.Dispose();
                Playback.Current.PlaybackSpeed = _previousPlaybackSpeed;
            }
        }

        CustomComponents.HelpText(_lastHelpString);
    }

    private static int GetRealFrame()
    {
        // since we are double-buffering and discarding the first few frames,
        // we have to subtract these frames to get the currently really shown framenumber...
        return FrameIndex;
    }

    private static string GetFilePath()
    {
        return Path.Combine(_targetFolder, $"output_{GetRealFrame():0000}.{Extension}");
    }

    private static bool SaveCurrentFrameAndAdvance(Texture2D mainTexture)
    {
        try
        {
            var success = SaveImage(mainTexture);
            FrameIndex++;
            SetPlaybackTimeForNextFrame();
            return success;
        }
        catch (Exception e)
        {
            _lastHelpString = e.ToString();
            _isExporting = false;
            return false;
        }
    }

    private static bool SaveImage(Texture2D mainTexture)
    {
        return ScreenshotWriter.StartSavingToFile(mainTexture, GetFilePath(), _fileFormat);
    }

    private static string Extension => _fileFormat.ToString().ToLower(); 

    private static double _exportStartedTime;
    private static string _targetFolder = "./Render";

    private static ScreenshotWriter.FileFormats _fileFormat;
    private static string _lastHelpString = string.Empty;
    private double _previousPlaybackSpeed;
}