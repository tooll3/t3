using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
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
    public class RenderVideoWindow : Window
    {
        public RenderVideoWindow()
        {
            Config.Title = "Render Video";
            Config.Size = new Vector2(350, 300);
            _lastHelpString = "Hint: Use a [RenderTarget] with format R8G8B8A8_UNorm for faster exports.";
        }

        protected override void UpdateBeforeDraw()
        {
            ImGui.SetNextWindowSize(new Vector2(550, 270));
        }

        protected override void DrawContent()
        {
            //ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, 10);
            CustomComponents.FloatValueEdit("Start in secs", ref _startTime);
            CustomComponents.FloatValueEdit("End in secs", ref _endTime);
            CustomComponents.FloatValueEdit("FPS", ref _fps);
            CustomComponents.IntValueEdit("bitrate", ref _bitrate, 0, 25000000, 1000);
            CustomComponents.StringValueEdit("File", ref _targetFile);
            ImGui.SameLine();
            FileOperations.DrawFileSelector(FileOperations.FilePickerTypes.File, ref _targetFile);

            var mainTexture = OutputWindow.GetPrimaryOutputWindow()?.GetCurrentTexture();
            if (mainTexture == null) return;

            var currentDesc = mainTexture.Description;
            SharpDX.Size2 size;
            size.Width = currentDesc.Width;
            size.Height = currentDesc.Height;
            _frameCount = (int)((_endTime - _startTime) * _fps) + 1;

            ImGui.Separator();

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

                        if (_videoWriter == null)
                        {
                            _videoWriter = new MP4VideoWriter(_targetFile, size);
                            _videoWriter.Bitrate = _bitrate;
                            _videoWriter.Framerate = (int)(_fps + 0.5f);
                        }

                        SaveCurrentFrameAndAdvance(ref mainTexture);
                    }
                }
            }
            else
            {
                var success = SaveCurrentFrameAndAdvance(ref mainTexture);
                ImGui.ProgressBar(Progress, new Vector2(-1, 4));

                if (_frameIndex > _frameCount)
                {
                    var currentTime = Playback.RunTimeInSecs;
                    var durationSoFar = currentTime - _exportStartedTime;
                    _lastHelpString = $"Video export finished successfully in {durationSoFar:0.00}s";
                }

                if (_frameIndex > _frameCount || !success || ImGui.Button("Cancel"))
                {
                    _isExporting = false;
                    _videoWriter?.Dispose();
                    _videoWriter = null;
                }
                else
                {
                    var currentTime = Playback.RunTimeInSecs;
                    var durationSoFar = currentTime - _exportStartedTime;
                    var estimatedTimeLeft = durationSoFar * (1 - Progress);
                    _lastHelpString = $"Saved {_videoWriter.FilePath} frame {_frameIndex}/{_frameCount}  ";
                    _lastHelpString += $"{Progress * 100.0:0}%  {estimatedTimeLeft:0}s left";
                }
            }

            CustomComponents.HelpText(_lastHelpString);
        }

        private static bool ValidateOrCreateTargetFolder()
        {
            string directory = Path.GetDirectoryName(_targetFile);
            if (File.Exists(_targetFile))
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


        private static bool SaveCurrentFrameAndAdvance(ref Texture2D mainTexture)
        {
            try
            {
                _videoWriter.AddVideoFrame(ref mainTexture);

                _frameIndex++;
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

        private static void SetPlaybackTimeForNextFrame()
        {
            Playback.Current.TimeInSecs = MathUtils.Lerp(_startTime, _endTime, Progress);
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        // private static string Extension => _videoWriter.ToString().ToLower(); 
        private static float Progress => (float)(_frameIndex / (double)_frameCount).Clamp(0, 1);

        private static double _exportStartedTime;
        private static bool _isExporting;
        private static float _startTime;
        private static float _endTime = 1.0f;
        private static float _fps = 60.0f;
        private static int _bitrate = 15000000;
        private static int _frameIndex;
        private static int _frameCount;
        private static string _targetFile = "./Render/output.mp4";

        private static MP4VideoWriter _videoWriter = null;
        private static string _lastHelpString = string.Empty;
    }
}