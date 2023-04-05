using System;
using ImGuiNET;
using SharpDX.Direct3D11;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.Output;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.Windows
{
    public class RenderVideoWindow : RenderHelperWindow
    {
        public RenderVideoWindow()
        {
            Config.Title = "Render Video";
            _lastHelpString = "Hint: Use a [RenderTarget] with format R8G8B8A8_UNorm for faster exports.";
        }


        protected override void DrawContent()
        {
            DrawTimeSetup();

            // custom parameters for this renderer
            FormInputs.AddInt("Bitrate", ref _bitrate, 0, 25000000, 1000);
            FormInputs.AddStringInput("File", ref _targetFile);
            ImGui.SameLine();
            FileOperations.DrawFileSelector(FileOperations.FilePickerTypes.File, ref _targetFile);
            ImGui.Separator();

            var mainTexture = OutputWindow.GetPrimaryOutputWindow()?.GetCurrentTexture();
            if (mainTexture == null)
            {
                CustomComponents.HelpText("You have selected an operator that does not render. " +
                                          "Hint: Use a [RenderTarget] with format R8G8B8A8_UNorm for fast exports.");
                return;
            }

            if (!_isExporting)
            {
                if (ImGui.Button("Start Export"))
                {
                    if (ValidateOrCreateTargetFolder(_targetFile))
                    {
                        _isExporting = true;
                        _exportStartedTime = Playback.RunTimeInSecs;
                        _frameIndex = 0;
                        SetPlaybackTimeForNextFrame();

                        if (_videoWriter == null)
                        {
                            var currentDesc = mainTexture.Description;
                            SharpDX.Size2 size;
                            size.Width = currentDesc.Width;
                            size.Height = currentDesc.Height;

                            _videoWriter = new MP4VideoWriter(_targetFile, size, true);
                            _videoWriter.Bitrate = _bitrate;
                            // FIXME: Allow floating point FPS in a future version
                            _videoWriter.Framerate = (int)_fps;
                        }

                        var audioFrame = AudioEngine.LastMixDownBuffer(0.0);
                        SaveCurrentFrameAndAdvance(ref mainTexture, ref audioFrame,
                                                    soundtrackChannels(), soundtrackSampleRate());
                    }
                }
            }
            else
            {
                // Save current frame and determine what to do next
                var audioFrame = AudioEngine.LastMixDownBuffer(Playback.LastFrameDuration);
                var success = SaveCurrentFrameAndAdvance(ref mainTexture, ref audioFrame,
                                                         soundtrackChannels(), soundtrackSampleRate());

                ImGui.ProgressBar(Progress, new Vector2(-1, 4));
                var currentTime = Playback.RunTimeInSecs;
                var durationSoFar = currentTime - _exportStartedTime;
                if (GetRealFrame() >= _frameCount || !success)
                {
                    if (success)
                        _lastHelpString = $"Sequence export of {_frameCount} frames finished successfully in {durationSoFar:0.00}s";
                    else
                        _lastHelpString = $"Sequence export finished unsuccessfully in {durationSoFar:0.00}s\n" + _lastHelpString;

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
                    _lastHelpString = $"Saved {_videoWriter.FilePath} frame {GetRealFrame()+1}/{_frameCount}  ";
                    _lastHelpString += $"{Progress * 100.0:0}%  {estimatedTimeLeft:0.0}s left";
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

        private static int GetRealFrame()
        {
            // since we are double-buffering and discarding the first few frames,
            // we have to subtract these frames to get the currently really shown framenumber...
            return _frameIndex - MediaFoundationVideoWriter.SkipImages;
        }

        private static bool SaveCurrentFrameAndAdvance(ref Texture2D mainTexture, ref byte[] audioFrame,
                                                       int channels, int sampleRate)
        {
            try
            {
                if (audioFrame != null)
                    _videoWriter.AddVideoAndAudioFrame(ref mainTexture, ref audioFrame, channels, sampleRate);
                else
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
                ReleasePlaybackTime();
                return false;
            }

            return true;
        }

        private static double _exportStartedTime;
        
        private static int _bitrate = 15000000;
        private static string _targetFile = "./Render/output.mp4";

        private static MP4VideoWriter _videoWriter = null;
        private static string _lastHelpString = string.Empty;
    }
}