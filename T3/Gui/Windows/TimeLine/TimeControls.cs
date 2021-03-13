using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ImGuiNET;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator.Slots;
using T3.Gui.Commands;
using T3.Gui.Graph;
using T3.Gui.Interaction.Timing;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;
using Icon = T3.Gui.Styling.Icon;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace T3.Gui.Windows.TimeLine
{
    internal static class TimeControls
    {
        internal static void DrawTimeControls(ref Playback playback, TimeLineCanvas timeLineCanvas)
        {
            // Current Time
            var delta = 0.0;
            string formattedTime = "";
            switch (UserSettings.Config.TimeDisplayMode)
            {
                case Playback.TimeDisplayModes.Bars:
                    formattedTime = $"{playback.Bar:0}. {playback.Beat:0}. {playback.Tick:0}.";
                    break;

                case Playback.TimeDisplayModes.Secs:
                    formattedTime = TimeSpan.FromSeconds(playback.TimeInSecs).ToString(@"hh\:mm\:ss\:ff");
                    break;

                case Playback.TimeDisplayModes.F30:
                    var frames = playback.TimeInSecs * 30;
                    formattedTime = $"{frames:0}f ";
                    break;
                case Playback.TimeDisplayModes.F60:
                    var frames60 = playback.TimeInSecs * 60;
                    formattedTime = $"{frames60:0}f ";
                    break;
            }

            if (CustomComponents.JogDial(formattedTime, ref delta, new Vector2(100, 0)))
            {
                playback.PlaybackSpeed = 0;
                playback.TimeInBars += delta;
                if (UserSettings.Config.TimeDisplayMode == Playback.TimeDisplayModes.F30)
                {
                    playback.TimeInSecs = Math.Floor(playback.TimeInSecs * 30) / 30;
                }
            }

            ImGui.SameLine();

            // Time Mode with context menu
            if (ImGui.Button(UserSettings.Config.TimeDisplayMode.ToString(), ControlSize))
            {
                UserSettings.Config.TimeDisplayMode =
                    (Playback.TimeDisplayModes)(((int)UserSettings.Config.TimeDisplayMode + 1) % Enum.GetNames(typeof(Playback.TimeDisplayModes)).Length);
            }

            CustomComponents.TooltipForLastItem("Timeline format",
                                                "Click to toggle through BPM, Frames and Normal time modes");

            DrawTimeSettingsContextMenu(ref playback);

            ImGui.SameLine();

            // Continue Beat indicator
            {
                ImGui.PushStyleColor(ImGuiCol.Text, UserSettings.Config.KeepBeatTimeRunningInPause
                                                        ? new Vector4(1, 1, 1, 0.5f)
                                                        : new Vector4(0, 0, 0, 0.5f));

                if (CustomComponents.IconButton(Icon.BeatGrid, "##continueBeat", ControlSize))
                {
                    UserSettings.Config.KeepBeatTimeRunningInPause = !UserSettings.Config.KeepBeatTimeRunningInPause;
                }

                CustomComponents.TooltipForLastItem("Keep beat time running",
                                                    "This will keep updating the output [Time]\nwhich is useful for procedural animation and syncing.");

                if (UserSettings.Config.KeepBeatTimeRunningInPause)
                {
                    var center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) / 2;
                    var beat = (int)(playback.BeatTime * 4) % 4;
                    var bar = (int)(playback.BeatTime) % 4;
                    const int gridSize = 4;
                    var drawList = ImGui.GetWindowDrawList();
                    var min = center - new Vector2(8, 9) + new Vector2(beat * gridSize, bar * gridSize);
                    drawList.AddRectFilled(min, min + new Vector2(gridSize - 1, gridSize - 1), Color.Orange);
                }

                ImGui.PopStyleColor();
                ImGui.SameLine();
            }

            var hideTimeControls = playback is BeatTimingPlayback;

            if (hideTimeControls)
            {
                if (ImGui.Button($"{T3Ui.BeatTiming.DampedBpm:0.0} BPM?"))
                {
                    T3Ui.BeatTiming.SetBpmFromSystemAudio();
                    // if (newBpm > 0)
                    //     playback.Bpm = newBpm;
                }

                var min = ImGui.GetItemRectMin();
                var max = ImGui.GetItemRectMax();
                //var volume = Im.Clamp(BpmDetection.LastVolume,0,1);
                var volume = BeatTiming.SyncPrecision;
                ImGui.GetWindowDrawList().AddRectFilled(new Vector2(min.X, max.Y), new Vector2(min.X + 3, max.Y - volume * (max.Y - min.Y)), Color.Orange);

                ImGui.SameLine();

                ImGui.Button("Sync");
                if (ImGui.IsItemActivated())
                {
                    T3Ui.BeatTiming.TriggerSyncTap();
                }
                else if (ImGui.IsItemHovered() && ImGui.IsWindowFocused() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                {
                    Log.Debug("Resync!");
                    T3Ui.BeatTiming.TriggerResyncMeasure();
                }

                CustomComponents.TooltipForLastItem("Click on beat to sync. Tap later once to refine. Click right to sync measure.",
                                                    KeyboardBinding.ListKeyboardShortcuts(UserActions.PlaybackJumpToStartTime));

                ImGui.SameLine();

                ImGui.PushButtonRepeat(true);
                {
                    if (ImGui.ArrowButton("##left", ImGuiDir.Left))
                    {
                        T3Ui.BeatTiming.TriggerDelaySync();
                    }

                    ImGui.SameLine();

                    if (ImGui.ArrowButton("##right", ImGuiDir.Right))
                    {
                        T3Ui.BeatTiming.TriggerAdvanceSync();
                    }

                    ImGui.SameLine();
                }
                ImGui.PopButtonRepeat();
            }
            else
            {
                // Jump to start
                if (CustomComponents.IconButton(Icon.JumpToRangeStart, "##jumpToBeginning", ControlSize))
                {
                    playback.TimeInBars = playback.LoopRange.Start;
                }

                CustomComponents.TooltipForLastItem("Jump to beginning",
                                                    KeyboardBinding.ListKeyboardShortcuts(UserActions.PlaybackJumpToStartTime));

                ImGui.SameLine();

                // Prev Keyframe
                if (CustomComponents.IconButton(Icon.JumpToPreviousKeyframe, "##prevKeyframe", ControlSize)
                    || KeyboardBinding.Triggered(UserActions.PlaybackJumpToPreviousKeyframe))
                {
                    UserActionRegistry.DeferredActions.Add(UserActions.PlaybackJumpToPreviousKeyframe);
                }

                CustomComponents.TooltipForLastItem("Jump to previous keyframe",
                                                    KeyboardBinding.ListKeyboardShortcuts(UserActions.PlaybackJumpToPreviousKeyframe));

                ImGui.SameLine();

                // Play backwards
                var isPlayingBackwards = playback.PlaybackSpeed < 0;
                if (CustomComponents.ToggleIconButton(Icon.PlayBackwards,
                                                      label: isPlayingBackwards ? $"{(int)playback.PlaybackSpeed}x##backwards" : "##backwards",
                                                      ref isPlayingBackwards,
                                                      ControlSize))
                {
                    if (playback.PlaybackSpeed != 0)
                    {
                        playback.PlaybackSpeed = 0;
                    }
                    else if (playback.PlaybackSpeed == 0)
                    {
                        playback.PlaybackSpeed = -1;
                    }
                }

                CustomComponents.TooltipForLastItem("Play backwards",
                                                    "Play backwards (and faster): " +
                                                    KeyboardBinding.ListKeyboardShortcuts(UserActions.PlaybackBackwards, false) +
                                                    "\n Previous frame:" + KeyboardBinding.ListKeyboardShortcuts(UserActions.PlaybackPreviousFrame, false));

                ImGui.SameLine();

                // Play forward
                var isPlaying = playback.PlaybackSpeed > 0;
                if (CustomComponents.ToggleIconButton(Icon.PlayForwards,
                                                      label: isPlaying ? $"{(int)playback.PlaybackSpeed}x##forward" : "##forward",
                                                      ref isPlaying,
                                                      ControlSize))
                {
                    if (Math.Abs(playback.PlaybackSpeed) > 0.001f)
                    {
                        playback.PlaybackSpeed = 0;
                    }
                    else if (Math.Abs(playback.PlaybackSpeed) < 0.001f)
                    {
                        playback.PlaybackSpeed = 1;
                    }
                }

                CustomComponents.TooltipForLastItem("Start playback",
                                                    "Play forward (and faster): " +
                                                    KeyboardBinding.ListKeyboardShortcuts(UserActions.PlaybackForward, false) +
                                                    "\n Play half speed (and slower): " +
                                                    KeyboardBinding.ListKeyboardShortcuts(UserActions.PlaybackForwardHalfSpeed, false) +
                                                    "\n Next frame:" + KeyboardBinding.ListKeyboardShortcuts(UserActions.PlaybackNextFrame, false));

                const float editFrameRate = 30;
                const float frameDuration = 1 / editFrameRate;

                // Step to previous frame
                if (KeyboardBinding.Triggered(UserActions.PlaybackPreviousFrame))
                {
                    var rounded = Math.Round(playback.TimeInBars * editFrameRate) / editFrameRate;
                    playback.TimeInBars = rounded - frameDuration;
                }

                // Step to next frame
                if (KeyboardBinding.Triggered(UserActions.PlaybackNextFrame))
                {
                    var rounded = Math.Round(playback.TimeInBars * editFrameRate) / editFrameRate;
                    playback.TimeInBars = rounded + frameDuration;
                }

                // Play backwards with increasing speed
                if (KeyboardBinding.Triggered(UserActions.PlaybackBackwards))
                {
                    Log.Debug("Backwards triggered with speed " + playback.PlaybackSpeed);
                    if (playback.PlaybackSpeed >= 0)
                    {
                        playback.PlaybackSpeed = -1;
                    }
                    else if (playback.PlaybackSpeed > -16)
                    {
                        playback.PlaybackSpeed *= 2;
                    }
                }

                // Play forward with increasing speed
                if (KeyboardBinding.Triggered(UserActions.PlaybackForward))
                {
                    if (playback.PlaybackSpeed <= 0)
                    {
                        playback.PlaybackSpeed = 1;
                    }
                    else if (playback.PlaybackSpeed < 16) // Bass can't play much faster anyways
                    {
                        playback.PlaybackSpeed *= 2;
                    }
                }

                if (KeyboardBinding.Triggered(UserActions.PlaybackForwardHalfSpeed))
                {
                    if (playback.PlaybackSpeed > 0 && playback.PlaybackSpeed < 1f)
                        playback.PlaybackSpeed *= 0.5f;
                    else
                        playback.PlaybackSpeed = 0.5f;
                }

                // Stop as separate keyboard action 
                if (KeyboardBinding.Triggered(UserActions.PlaybackStop))
                {
                    playback.PlaybackSpeed = 0;
                }

                if (KeyboardBinding.Triggered(UserActions.PlaybackToggle))
                {
                    playback.PlaybackSpeed = playback.PlaybackSpeed == 0 ? 1 : 0;
                }

                ImGui.SameLine();

                // Next Keyframe
                if (CustomComponents.IconButton(Icon.JumpToNextKeyframe, "##nextKeyframe", ControlSize)
                    || KeyboardBinding.Triggered(UserActions.PlaybackJumpToNextKeyframe))
                {
                    UserActionRegistry.DeferredActions.Add(UserActions.PlaybackJumpToNextKeyframe);
                }

                CustomComponents.TooltipForLastItem("Jump to next keyframe",
                                                    KeyboardBinding.ListKeyboardShortcuts(UserActions.PlaybackJumpToNextKeyframe));
                ImGui.SameLine();

                // // End
                // if (CustomComponents.IconButton(Icon.JumpToRangeEnd, "##lastKeyframe", ControlSize))
                // {
                //     playback.TimeInBars = playback.LoopRange.End;
                // }
                //
                // ImGui.SameLine();
                // Loop
                if (CustomComponents.ToggleIconButton(Icon.Loop, "##loop", ref playback.IsLooping, ControlSize))
                {
                    var loopRangeMatchesTime = playback.LoopRange.IsValid && playback.LoopRange.Contains(playback.TimeInBars);
                    if (playback.IsLooping && !loopRangeMatchesTime)
                    {
                        playback.LoopRange.Start = (float)(playback.TimeInBars - playback.TimeInBars % 4);
                        playback.LoopRange.Duration = 4;
                    }
                }

                CustomComponents.TooltipForLastItem("Loop playback", "This will initialize one bar around current time.");

                ImGui.SameLine();
            }

            // Curve Mode
            if (ImGui.Button(timeLineCanvas.Mode.ToString(), ControlSize))
            {
                timeLineCanvas.Mode = (TimeLineCanvas.Modes)(((int)timeLineCanvas.Mode + 1) % Enum.GetNames(typeof(TimeLineCanvas.Modes)).Length);
            }

            CustomComponents.TooltipForLastItem("Toggle keyframe view between Dope sheet and Curve mode.");

            ImGui.SameLine();

            // ToggleAudio
            if (CustomComponents.IconButton(UserSettings.Config.AudioMuted ? Icon.ToggleAudioOff : Icon.ToggleAudioOn, "##audioToggle", ControlSize))
            {
                UserSettings.Config.AudioMuted = !UserSettings.Config.AudioMuted;
                if (playback is StreamPlayback streamPlayback)
                    streamPlayback.SetMuteMode(UserSettings.Config.AudioMuted);
            }

            ImGui.SameLine();

            // ToggleHover
            Icon icon;
            string tooltip;
            string additionalTooltip = null;
            switch (UserSettings.Config.HoverMode)
            {
                case GraphCanvas.HoverModes.Disabled:
                    icon = Icon.HoverPreviewDisabled;
                    tooltip = "No preview images on hover";
                    break;
                case GraphCanvas.HoverModes.Live:
                    icon = Icon.HoverPreviewPlay;
                    tooltip = "Live Hover Preview - Render explicit thumbnail image.";
                    additionalTooltip = "This can interfere with the rendering of the current output.";
                    break;
                default:
                    icon = Icon.HoverPreviewSmall;
                    tooltip = "Last - Show the current state of the operator.";
                    additionalTooltip = "This can be outdated if operator is not require for current output.";
                    break;
            }

            if (CustomComponents.IconButton(icon, "##hoverPreview", ControlSize))
            {
                UserSettings.Config.HoverMode =
                    (GraphCanvas.HoverModes)(((int)UserSettings.Config.HoverMode + 1) % Enum.GetNames(typeof(GraphCanvas.HoverModes)).Length);
            }

            CustomComponents.TooltipForLastItem(tooltip, additionalTooltip);


        }

        private static void DrawTimeSettingsContextMenu(ref Playback playback)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6));
            ImGui.SetNextWindowSize(new Vector2(400, 300));
            if (!ImGui.BeginPopupContextItem("##TimeSettings"))
            {
                ImGui.PopStyleVar(2);
                return;
            }

            T3Ui.OpenedPopUpName = "##TimeSettings";

            ImGui.PushFont(Fonts.FontLarge);
            ImGui.Text("Playback settings");
            ImGui.PopFont();

            if (ImGui.BeginTabBar("##timeMode"))
            {
                if (ImGui.BeginTabItem("AudioFile"))
                {
                    ImGui.Text("Soundtrack");
                    var filepathModified =
                        FileOperations.DrawSoundFilePicker(FileOperations.FilePickerTypes.File, ref ProjectSettings.Config.SoundtrackFilepath);

                    var isInitialized = playback is StreamPlayback;
                    if (isInitialized)
                    {
                        var bpm = (float)playback.Bpm;

                        if (ImGui.Checkbox("Use BPM Rate", ref ProjectSettings.Config.UseBpmRate))
                        {
                            if (!ProjectSettings.Config.UseBpmRate)
                                UserSettings.Config.TimeDisplayMode = Playback.TimeDisplayModes.Secs;
                        }

                        if (ProjectSettings.Config.UseBpmRate)
                        {
                            ImGui.DragFloat("BPM", ref bpm);
                            playback.Bpm = bpm;
                            ProjectSettings.Config.SoundtrackBpm = bpm;
                        }

                        if (filepathModified)
                        {
                            var matchBpmPattern = new Regex(@"(\d+\.?\d*)bpm");
                            var result = matchBpmPattern.Match(ProjectSettings.Config.SoundtrackFilepath);
                            if (result.Success)
                            {
                                float.TryParse(result.Groups[1].Value, out bpm);
                                ProjectSettings.Config.SoundtrackBpm = bpm;
                                playback.Bpm = bpm;
                            }

                            var job = new AsyncImageGenerator(ProjectSettings.Config.SoundtrackFilepath);
                            job.Run();
                        }
                    }

                    if (ImGui.Button(isInitialized ? "Update" : "Load"))
                    {
                        if (playback is StreamPlayback streamPlayback)
                        {
                            streamPlayback.LoadFile(ProjectSettings.Config.SoundtrackFilepath);
                        }
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Tapping"))
                {
                    CustomComponents.HelpText("Tab the Sync button to set begin of measure and to improve BPM detection.");
                    var isInitialized = playback is BeatTimingPlayback;
                    if (isInitialized)
                    {
                    }
                    else
                    {
                        if (ImGui.Button("Initialize"))
                        {
                            playback = new BeatTimingPlayback();
                        }
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("System Audio"))
                {
                    CustomComponents.HelpText("Uses Windows core audio input for BPM detection");
                    CustomComponents.HelpText("Tab the Sync button to set begin of measure and to improve BPM detection.");
                    var isInitialized = playback is BeatTimingPlayback && T3Ui.BeatTiming.UseSystemAudio;
                    if (isInitialized)
                    {
                        var currentDevice = BeatTiming.SystemAudioInput.LoopBackDevices[BeatTiming.SystemAudioInput.SelectedDeviceIndex];
                        if (ImGui.BeginCombo("Device selection", currentDevice.ToString()))
                        {
                            for (var index = 0; index < BeatTiming.SystemAudioInput.LoopBackDevices.Count; index++)
                            {
                                var d = BeatTiming.SystemAudioInput.LoopBackDevices[index];
                                if (ImGui.Selectable(d.ToString()))
                                {
                                    BeatTiming.SystemAudioInput.SetDeviceIndex(index);
                                }
                            }

                            ImGui.EndCombo();
                        }
                    }
                    else
                    {
                        if (ImGui.Button("Initialize"))
                        {
                            playback = new BeatTimingPlayback();
                            T3Ui.BeatTiming.UseSystemAudio = true;
                        }

                        CustomComponents.HelpText("This can take several seconds...");
                    }

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.EndPopup();
            ImGui.PopStyleVar(2);
        }

        private class AsyncImageGenerator
        {
            public AsyncImageGenerator(string filepath)
            {
                if (FilePathsInProgress.Contains(filepath))
                {
                    Log.Debug($"Skipping {filepath} because its being processed right now.");
                    return;
                }

                _generator = new SoundImageGenerator(filepath);
            }

            public void Run()
            {
                FilePathsInProgress.Add(_generator.Filepath);
                Task.Run(GenerateAsync);
            }

            private void GenerateAsync()
            {
                var imageFilePath = _generator.GenerateSoundSpectrumAndVolume();
                if (imageFilePath == null)
                {
                    Log.Debug("could not create filepath");
                }

                TimeLineImage.LoadSoundImage();
                FilePathsInProgress.Remove(_generator.Filepath);
            }

            private readonly SoundImageGenerator _generator;
            private static readonly HashSet<string> FilePathsInProgress = new HashSet<string>();
        }

        public static readonly Vector2 ControlSize = new Vector2(45, 26);
    }
}