using System;
using System.IO;
using ImGuiNET;
using SharpDX.Direct3D11;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.Output;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.Windows.RenderExport;

public class RenderSequenceWindow : BaseRenderWindow
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

        if (!IsExportingImages && !IsToollRenderingSomething)
        {
            if (ImGui.Button("Start Export"))
            {
                if (ValidateOrCreateTargetFolder(_targetFolder))
                {
                    IsExportingImages = true;
                    _exportStartedTime = Playback.RunTimeInSecs;
                    FrameIndex = 0;
                    SetPlaybackTimeForThisFrame();
                    TextureReadAccess.ClearQueue();
                }
            }
        }
        else if(IsExportingImages)
        {
            // Handle audio although we do not save it
            AudioEngine.LastMixDownBuffer(Playback.LastFrameDuration);
            var success = SaveCurrentFrameAndAdvance(mainTexture);
            ImGui.ProgressBar((float) Progress, new Vector2(-1, 4));

            var currentTime = Playback.RunTimeInSecs;
            var durationSoFar = currentTime - _exportStartedTime;
            if (FrameIndex >= FrameCount + 2 || !success)
            {
                var successful = success ? "successfully" : "unsuccessfully";
                _lastHelpString = $"Sequence export finished {successful} in {durationSoFar:0.00}s";
                IsExportingImages = false;
            }
            else if (ImGui.Button("Cancel"))
            {
                _lastHelpString = $"Sequence export cancelled after {durationSoFar:0.00}s";
                IsExportingImages = false;
            }
            else
            {
                var estimatedTimeLeft = durationSoFar / Progress - durationSoFar;
                _lastHelpString = $"Saved {ScreenshotWriter.LastFilename} frame {FrameIndex+1}/{FrameCount}  ";
                _lastHelpString += $"{Progress * 100.0:0}%%  {estimatedTimeLeft:0}s left";
            }
            
            if (!IsExportingImages)
            {
                ReleasePlaybackTime();
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
            SetPlaybackTimeForThisFrame();
            return success;
        }
        catch (Exception e)
        {
            _lastHelpString = e.ToString();
            IsExportingImages = false;
            return false;
        }
    }
    
    private static bool IsExportingImages
    {
        get => _isExporting2;
        set
        {
            if (value)
            {
                SetRenderingStarted();
            }
            else
            {
                RenderingFinished();
            }

            _isExporting2 = value;
        }
    }
    private static bool _isExporting2;

    private static string Extension => _fileFormat.ToString().ToLower(); 

    private static double _exportStartedTime;
    private static string _targetFolder = UserSettings.Config.RenderSequenceFilePath;

    private static ScreenshotWriter.FileFormats _fileFormat;
    private static string _lastHelpString = string.Empty;
}