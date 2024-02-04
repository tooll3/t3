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

        FormInputs.AddEnumDropdown(ref _fileFormat, "FileFormat");
        FormInputs.AddStringInput("Folder", ref _targetFolder);
        ImGui.SameLine();
        FileOperations.DrawFileSelector(FileOperations.FilePickerTypes.Folder, ref _targetFolder);
        ImGui.Separator();

        var mainTexture = OutputWindow.GetPrimaryOutputWindow()?.GetCurrentTexture();

        if (!IsExporting)
        {
            if (ImGui.Button("Start Export"))
            {
                if (ValidateOrCreateTargetFolder(_targetFolder))
                {
                    _previousPlaybackSpeed = Playback.Current.PlaybackSpeed;
                    Playback.Current.PlaybackSpeed = 0;
                    _isExporting = true;
                    _exportStartedTime = Playback.RunTimeInSecs;
                    FrameIndex = 0;
                    SetPlaybackTimeForNextFrame();
                    TextureReadAccess.ClearQueue();
                }
            }
        }
        else
        {
            // This is a very unfortunate hack. Sadly, activating playback can interfer
            // with precise frame positioning will be required for exporting audio-reactivity...
            if(FrameIndex>0)
                Playback.Current.PlaybackSpeed = 1;
            
            var success = SaveCurrentFrameAndAdvance(mainTexture);
            ImGui.ProgressBar(Progress, new Vector2(-1, 4));

            var currentTime = Playback.RunTimeInSecs;
            var durationSoFar = currentTime - _exportStartedTime;
            
            if (FrameIndex  >= FrameCount +2 || !success)
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
                var estimatedTimeLeft = durationSoFar / Progress - durationSoFar;
                _lastHelpString = $"Saved {ScreenshotWriter.LastFilename} frame {FrameIndex+1}/{FrameCount}  ";
                _lastHelpString += $"{Progress * 100.0:0} %%  {estimatedTimeLeft:0}s left";            }

            if (!_isExporting)
            {
                Playback.Current.PlaybackSpeed = _previousPlaybackSpeed;
            }
        }

        CustomComponents.HelpText(_lastHelpString);
    }

    private static string GetFilePath()
    {
        return Path.Combine(_targetFolder, $"output_{FrameIndex:0000}.{Extension}");
    }

    private static bool SaveCurrentFrameAndAdvance(Texture2D mainTexture)
    {
        try
        {
            var success = ScreenshotWriter.StartSavingToFile(mainTexture, GetFilePath(), _fileFormat);
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

    private static string Extension => _fileFormat.ToString().ToLower(); 

    private static double _exportStartedTime;
    private static string _targetFolder = "./Render";

    private static ScreenshotWriter.FileFormats _fileFormat;
    private static string _lastHelpString = string.Empty;
    private double _previousPlaybackSpeed;
}