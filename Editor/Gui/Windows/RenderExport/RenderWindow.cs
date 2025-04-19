using System;
using System.IO;
using System.Text.RegularExpressions;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.DataTypes;
using T3.Core.DataTypes.Vector;
using T3.Core.Utils;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.Output;
using T3.Editor.UiModel.ProjectHandling;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.Windows.RenderExport
{
    internal sealed class RenderWindow : BaseRenderWindow
    {
        internal RenderWindow()
        {
            Config.Title = "Render";
            _lastHelpString = PreferredInputFormatHint;
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
                _lastHelpString = warning; // Update _lastHelpString to persist the warning
                CustomComponents.HelpText(warning);
                return;
            }

            // Render Mode Selection
            FormInputs.AddVerticalSpace();
            FormInputs.AddSegmentedButtonWithLabel(ref _renderMode, "Render Mode");

            // Common Output Settings
            Int2 size = default;
            if (mainTexture != null)
            {
                var currentDesc = mainTexture.Description;
                size.Width = currentDesc.Width;
                size.Height = currentDesc.Height;
            }

            FormInputs.AddVerticalSpace();

            // Mode-Specific Settings
            DrawModeSpecificSettings(size);

            FormInputs.AddVerticalSpace(5);
            ImGui.Separator();
            FormInputs.AddVerticalSpace(5);

            // Rendering Logic
            HandleRenderingProcess(ref mainTexture, size);

            CustomComponents.HelpText(_lastHelpString);
        }

        private void DrawModeSpecificSettings(Int2 size)
        {
            if (_renderMode == RenderMode.Video)
            {
                DrawVideoSettings(size);
            }
            else // RenderMode.ImageSequence
            {
                DrawImageSequenceSettings();
            }
        }

        private void DrawVideoSettings(Int2 size)
        {
            FormInputs.AddInt("Bitrate", ref _bitrate, 0, 25000000, 1000);
            var duration = FrameCount / Fps;
            double bitsPerPixelSecond = _bitrate / (size.Width * size.Height * Fps);
            var q = GetQualityLevelFromRate((float)bitsPerPixelSecond);
            FormInputs.AddHint($"{q.Title} quality ({_bitrate * duration / 1024 / 1024 / 8:0} MB for {duration / 60:0}:{duration % 60:00}s at {size.Width}×{size.Height})");
            CustomComponents.TooltipForLastItem(q.Description);

            FormInputs.AddStringInput("Filename", ref UserSettings.Config.RenderVideoFilePath);
            ImGui.SameLine();
            FileOperations.DrawFileSelector(FileOperations.FilePickerTypes.File, ref UserSettings.Config.RenderVideoFilePath);

            if (IsFilenameIncrementable())
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, _autoIncrementVersionNumber ? 0.7f : 0.3f);
                FormInputs.AddCheckBox("Increment version after export", ref _autoIncrementVersionNumber);
                ImGui.PopStyleVar();
            }

            FormInputs.AddCheckBox("Export Audio (experimental)", ref _exportAudio);
        }

        private static void DrawImageSequenceSettings()
        {
            FormInputs.AddEnumDropdown(ref _fileFormat, "File Format");

            // Ensure the filename is trimmed and not empty
            if (FormInputs.AddStringInput("File name", ref UserSettings.Config.RenderSequenceFileName))
            {
                UserSettings.Config.RenderSequenceFileName = UserSettings.Config.RenderSequenceFileName?.Trim();
                if (string.IsNullOrEmpty(UserSettings.Config.RenderSequenceFileName))
                {
                    UserSettings.Config.RenderSequenceFileName = "output";
                }
            }
            // Add tooltip when hovering over the "File name" input field
            if (ImGui.IsItemHovered())
            {
                CustomComponents.TooltipForLastItem("Base filename for the image sequence (e.g., 'frame' for 'frame_0000.png').\n" +
                                 "Invalid characters (?, |, \", /, \\, :) will be replaced with underscores.\n" +
                                 "If empty, defaults to 'output'.");
            }

            // Use the existing UserSettings property for sequence path
            FormInputs.AddStringInput("Output Path", ref UserSettings.Config.RenderSequenceFilePath);
            if (ImGui.IsItemHovered())
            {
                CustomComponents.TooltipForLastItem("Specify the folder where the image sequence will be saved.\n" +
                                 "Must be a valid directory path.");
            }
            ImGui.SameLine();
            FileOperations.DrawFileSelector(FileOperations.FilePickerTypes.Folder, ref UserSettings.Config.RenderSequenceFilePath);

            // Add version increment option for image sequences too
            if (IsFilenameIncrementable(UserSettings.Config.RenderSequenceFilePath))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, _autoIncrementFolderVersionNumber ? 0.7f : 0.3f);
                FormInputs.AddCheckBox("Increment version after export", ref _autoIncrementFolderVersionNumber);
                ImGui.PopStyleVar();
            }
        }

        private void HandleRenderingProcess(ref Texture2D mainTexture, Int2 size)
        {
            if (!IsExporting && !IsToollRenderingSomething)
            {
                if (ImGui.Button("Start Render"))
                {
                    string targetPath = GetTargetPath();

                    if (ValidateOrCreateTargetFolder(targetPath))
                    {
                        StartRenderingProcess(targetPath, size);
                    }
                }
            }
            else if (IsExporting)
            {
                bool success = ProcessCurrentFrame(ref mainTexture, size);
                DisplayRenderingProgress(success);
            }
        }

        private string GetTargetPath()
        {
            return _renderMode == RenderMode.Video
                ? ResolveProjectRelativePath(UserSettings.Config.RenderVideoFilePath)
                : ResolveProjectRelativePath(UserSettings.Config.RenderSequenceFilePath);
        }

        private string ResolveProjectRelativePath(string path)
        {
            // Handle project-relative paths for both video and image sequence modes
            var project = ProjectView.Focused?.OpenedProject;
            if (project != null && path.StartsWith('.'))
            {
                return Path.Combine(project.Package.Folder, path);
            }

            return path.StartsWith('.')
                ? Path.Combine(UserSettings.Config.DefaultNewProjectDirectory, "Render", path)
                : path;
        }

        private void StartRenderingProcess(string targetPath, Int2 size)
        {
            IsExporting = true;
            _exportStartedTime = Playback.RunTimeInSecs;
            FrameIndex = 0;
            SetPlaybackTimeForThisFrame();

            if (_renderMode == RenderMode.Video && _videoWriter == null)
            {
                _videoWriter = new Mp4VideoWriter(targetPath, size, _exportAudio);
                _videoWriter.Bitrate = _bitrate;
                _videoWriter.Framerate = (int)Fps;
            }
            else if (_renderMode == RenderMode.ImageSequence)
            {
                _targetFolder = targetPath;
            }

            TextureReadAccess.ClearQueue();
        }

        private bool ProcessCurrentFrame(ref Texture2D mainTexture, Int2 size)
        {
            if (_renderMode == RenderMode.Video)
            {
                var audioFrame = AudioRendering.GetLastMixDownBuffer(1.0 / Fps);
                return SaveVideoFrameAndAdvance(ref mainTexture, ref audioFrame, SoundtrackChannels(), SoundtrackSampleRate());
            }
            else
            {
                AudioRendering.GetLastMixDownBuffer(Playback.LastFrameDuration);
                return SaveImageFrameAndAdvance(mainTexture);
            }
        }

        private void DisplayRenderingProgress(bool success)
        {
            ImGui.ProgressBar((float)Progress, new Vector2(-1, 4));

            var currentTime = Playback.RunTimeInSecs;
            var durationSoFar = currentTime - _exportStartedTime;

            int effectiveFrameCount = _renderMode == RenderMode.Video ? FrameCount : FrameCount + 2;
            int currentFrame = _renderMode == RenderMode.Video ? GetRealFrame() : FrameIndex + 1;

            if (currentFrame >= effectiveFrameCount || !success)
            {
                FinishRendering(success, durationSoFar);
            }
            else if (ImGui.Button("Cancel"))
            {
                _lastHelpString = $"Render cancelled after {StringUtils.HumanReadableDurationFromSeconds(durationSoFar)}";
                CleanupRendering();
            }
            else
            {
                UpdateProgressMessage(durationSoFar, currentFrame);
            }
        }

        private void FinishRendering(bool success, double durationSoFar)
        {
            var successful = success ? "successfully" : "unsuccessfully";
            _lastHelpString = $"Render finished {successful} in {StringUtils.HumanReadableDurationFromSeconds(durationSoFar)}\n Ready to render.";

            if (_renderMode == RenderMode.Video)
                TryIncrementingFileName();
            else if (_renderMode == RenderMode.ImageSequence && _autoIncrementFolderVersionNumber)
                TryIncrementingFolderName();

            CleanupRendering();
        }

        private void CleanupRendering()
        {
            IsExporting = false;
            if (_renderMode == RenderMode.Video)
            {
                _videoWriter?.Dispose();
                _videoWriter = null;
            }
            ReleasePlaybackTime();
        }

        private void UpdateProgressMessage(double durationSoFar, int currentFrame)
        {
            var estimatedTimeLeft = durationSoFar / Progress - durationSoFar;
            _lastHelpString = _renderMode == RenderMode.Video
                ? $"Saved {_videoWriter.FilePath} frame {currentFrame}/{FrameCount}  "
                : $"Saved {ScreenshotWriter.LastFilename} frame {currentFrame}/{FrameCount}  ";
            _lastHelpString += $"{Progress * 100.0:0}%%  {StringUtils.HumanReadableDurationFromSeconds(estimatedTimeLeft)} left";
        }

        // Video-specific methods
        private static int GetRealFrame() => FrameIndex - MfVideoWriter.SkipImages;

        private static bool SaveVideoFrameAndAdvance(ref Texture2D mainTexture, ref byte[] audioFrame, int channels, int sampleRate)
        {
            if (Playback.OpNotReady)
            {
                Log.Debug("Waiting for operators to complete");
                return true;
            }
            try
            {
                var savedFrame = _videoWriter.ProcessFrames(ref mainTexture, ref audioFrame, channels, sampleRate);
                FrameIndex++;
                SetPlaybackTimeForThisFrame();
                return true;
            }
            catch (Exception e)
            {
                _lastHelpString = e.ToString();
                IsExporting = false;
                _videoWriter?.Dispose();
                _videoWriter = null;
                ReleasePlaybackTime();
                return false;
            }
        }
        // Image sequence-specific methods

        private static string SanitizeFilename(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return "output";

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = filename;
            foreach (var c in invalidChars)
            {
                sanitized = sanitized.Replace(c.ToString(), "_");
            }
            return sanitized.Trim();
        }

        private static string GetFilePath()
        {
            var prefix = SanitizeFilename(UserSettings.Config.RenderSequenceFileName);
            return Path.Combine(_targetFolder, $"{prefix}_{FrameIndex:0000}.{_fileFormat.ToString().ToLower()}");
        }

        private static bool SaveImageFrameAndAdvance(Texture2D mainTexture)
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
                IsExporting = false;
                return false;
            }
        }

        // File path utilities
        private static readonly Regex _matchFileVersionPattern = new Regex(@"\bv(\d{2,4})\b");

        

        private static bool IsFilenameIncrementable(string path = null)
        {
            var filename = Path.GetFileName(path ?? UserSettings.Config.RenderVideoFilePath);
            return !string.IsNullOrEmpty(filename) && _matchFileVersionPattern.Match(filename).Success;
        }

        private static void TryIncrementingFileName()
        {
            if (!_autoIncrementVersionNumber) return;

            var filename = Path.GetFileName(UserSettings.Config.RenderVideoFilePath);
            if (string.IsNullOrEmpty(filename)) return;

            var result = _matchFileVersionPattern.Match(filename);
            if (!result.Success) return;

            var versionString = result.Groups[1].Value;
            if (!int.TryParse(versionString, out var versionNumber)) return;

            var digits = versionString.Length.Clamp(2, 4);
            var newVersionString = "v" + (versionNumber + 1).ToString("D" + digits);
            var newFilename = filename.Replace("v" + versionString, newVersionString);

            var directoryName = Path.GetDirectoryName(UserSettings.Config.RenderVideoFilePath);
            UserSettings.Config.RenderVideoFilePath = directoryName == null
                ? newFilename
                : Path.Combine(directoryName, newFilename);
        }

        private static void TryIncrementingFolderName()
        {
            if (!_autoIncrementFolderVersionNumber) return;

            var folderName = Path.GetFileName(UserSettings.Config.RenderSequenceFilePath);
            if (string.IsNullOrEmpty(folderName)) return;

            var result = _matchFileVersionPattern.Match(folderName);
            if (!result.Success) return;

            var versionString = result.Groups[1].Value;
            if (!int.TryParse(versionString, out var versionNumber)) return;

            var digits = versionString.Length.Clamp(2, 4);
            var newVersionString = "v" + (versionNumber + 1).ToString("D" + digits);
            var newFolderName = folderName.Replace("v" + versionString, newVersionString);

            var parentDirectory = Path.GetDirectoryName(UserSettings.Config.RenderSequenceFilePath);
            UserSettings.Config.RenderSequenceFilePath = parentDirectory == null
                ? newFolderName
                : Path.Combine(parentDirectory, newFolderName);
        }

        // Quality level for video
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

        // State
        private static bool IsExporting
        {
            get => _isExporting;
            set
            {
                if (value) SetRenderingStarted();
                else RenderingFinished();
                _isExporting = value;
            }
        }
        private static bool _isExporting;

        private enum RenderMode
        {
            Video,
            ImageSequence
        }

        private static RenderMode _renderMode = RenderMode.Video;
        private static int _bitrate = 25000000;
        private static bool _autoIncrementVersionNumber = true;
        private static bool _autoIncrementFolderVersionNumber = true;
        private static bool _exportAudio = true;
        private static Mp4VideoWriter _videoWriter;
        private static ScreenshotWriter.FileFormats _fileFormat;
        private static string _targetFolder = string.Empty;
        private static double _exportStartedTime;
        private static string _lastHelpString = string.Empty;

    }
}