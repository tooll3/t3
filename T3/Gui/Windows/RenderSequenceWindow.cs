using System;
using System.Collections.Generic;
using System.IO;
using ImGuiNET;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Gui.UiHelpers;
using T3.Gui.Windows.Output;
using Vector2 = System.Numerics.Vector2;

namespace T3.Gui.Windows
{
    public class RenderSequenceWindow : Window
    {
        public RenderSequenceWindow()
        {
            Config.Title = "Render Sequence";
            Config.Size = new Vector2(350, 300);
        }

        private static string _targetFolder = "./Render"; 
        protected override void DrawContent()
        {
            //ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, 10);
            CustomComponents.FloatValueEdit("Start in secs", ref _startTime);
            CustomComponents.FloatValueEdit("End in secs", ref _endTime);
            CustomComponents.FloatValueEdit("FPS", ref _fps);

            CustomComponents.StringValueEdit("Folder", ref _targetFolder);
            ImGui.SameLine();
            FileOperations.DrawFileSelector(FileOperations.FilePickerTypes.Folder, ref _targetFolder);

            var intFileFormat = (int)_fileFormat;
            if (CustomComponents.DrawEnumSelector<ScreenshotWriter.FileFormats>(ref intFileFormat, "FileFormat"))
            {
                _fileFormat = (ScreenshotWriter.FileFormats)intFileFormat;
            }

            _frameCount = (int)((_endTime - _startTime) * _fps) + 1;
            CustomComponents.HelpText($"That's {_frameCount} frames");

            ImGui.Separator();

            var mainTexture = OutputWindow.GetPrimaryOutputWindow()?.GetCurrentTexture();
            if (!_isExporting)
            {
                if (ImGui.Button("Start Export"))
                {
                    if (ValidateOrCreateTargetFolder())
                    {
                        _isExporting = true;
                        _exportStartedTime = Playback.RunTimeInSecs;
                        _frameIndex = 0;
                        SetPlaybackTimeForNextFrame();
                        
                        SaveImage(mainTexture);
                    }
                }
            }
            else
            {
                var success = SaveCurrentFrameAndAdvance(mainTexture);
                ImGui.ProgressBar(Progress, new Vector2(-1, 4));

                if (_frameIndex > _frameCount || !success)
                {
                    _isExporting = false;
                }
                else
                {
                    if (ImGui.Button("Cancel"))
                    {
                        _isExporting = false;
                    }
                }

                var currentTime = Playback.RunTimeInSecs;
                var durationSoFar = currentTime - _exportStartedTime;
                var estimatedTimeLeft = durationSoFar * (1 - Progress);
                CustomComponents.HelpText($"Saved {ScreenshotWriter.LastFilename}  {Progress * 100.0:0}%  {estimatedTimeLeft:0}s left");
            }
        }



        private static string GetFilePath()
        {
            return Path.Combine(_targetFolder, $"output_{_frameIndex:0000}.{Extension}");
        }

        private static bool ValidateOrCreateTargetFolder()
        {
            if (!Directory.Exists(_targetFolder))
            {
                try
                {
                    Directory.CreateDirectory(_targetFolder);
                }
                catch (Exception e)
                {
                    Log.Warning($"Failed to create target folder '{_targetFolder}': {e.Message}");
                    return false;
                }
            }
            return true;
        }

        
        private static bool SaveCurrentFrameAndAdvance(Texture2D mainTexture)
        {
            var success = SaveImage(mainTexture);
            

            _frameIndex++;
            SetPlaybackTimeForNextFrame();
            return success;
        }
        
        private static bool SaveImage(Texture2D mainTexture)
        {
            return ScreenshotWriter.SaveBufferToFile(mainTexture, GetFilePath(), _fileFormat);
        }

        private static void SetPlaybackTimeForNextFrame()
        {
            Playback.Current.TimeInSecs = MathUtils.Lerp(_startTime, _endTime, Progress);
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        private static string Extension => _fileFormat.ToString().ToLower(); 
        private static float Progress => (float)(_frameIndex / (double)_frameCount).Clamp(0, 1);

        private static double _exportStartedTime;
        private static bool _isExporting;
        private static float _startTime;
        private static float _endTime = 1;
        private static float _fps = 60;
        private static int _frameIndex;
        private static int _frameCount;

        private static ScreenshotWriter.FileFormats _fileFormat;
    }
}