using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator.Slots;
using T3.Gui;
using T3.Gui.Commands;
using T3.Gui.Graph;
using T3.Gui.UiHelpers;
using T3.Gui.Windows.TimeLine;
using Icon = T3.Gui.Styling.Icon;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace T3.Gui.Graph
{
    internal static class TimeControls
    {
        internal static void DrawTimeControls(Playback playback, TimeLineCanvas timeLineCanvas)
        {
            // Current Time
            var delta = 0.0;
            string formattedTime = "";
            switch (playback.TimeMode)
            {
                case Playback.TimeModes.Bars:
                    formattedTime = $"{playback.Bar:0}. {playback.Beat:0}. {playback.Tick:0}.";
                    break;

                case Playback.TimeModes.Secs:
                    formattedTime = TimeSpan.FromSeconds(playback.TimeInSecs).ToString(@"hh\:mm\:ss\:ff");
                    break;

                case Playback.TimeModes.F30:
                    var frames = playback.TimeInSecs * 30;
                    formattedTime = $"{frames:0}f ";
                    break;
                case Playback.TimeModes.F60:
                    var frames60 = playback.TimeInSecs * 60;
                    formattedTime = $"{frames60:0}f ";
                    break;
            }

            if (CustomComponents.JogDial(formattedTime, ref delta, new Vector2(100, 0)))
            {
                playback.PlaybackSpeed = 0;
                playback.TimeInBars += delta;
                if (playback.TimeMode == Playback.TimeModes.F30)
                {
                    playback.TimeInSecs = Math.Floor(playback.TimeInSecs * 30) / 30;
                }
            }

            ImGui.SameLine();

            // Time Mode with context menu
            if (ImGui.Button(playback.TimeMode.ToString(), ControlSize))
            {
                playback.TimeMode = (Playback.TimeModes)(((int)playback.TimeMode + 1) % Enum.GetNames(typeof(Playback.TimeModes)).Length);
            }

            ImGui.SetNextWindowSize(new Vector2(400, 200));
            CustomComponents.ContextMenuForItem(() =>
                                                {
                                                    var bpm = (float)playback.Bpm;
                                                    ImGui.DragFloat("BPM", ref bpm);
                                                    playback.Bpm = bpm;
                                                    ProjectSettings.Config.SoundtrackBpm = bpm;

                                                    ImGui.Text("Soundtrack");
                                                    var modified = FileOperations.DrawFilePicker(FileOperations.FilePickerTypes.File,
                                                                                                 ref ProjectSettings.Config.SoundtrackFilepath);

                                                    if (modified)
                                                    {
                                                        var matchBpmPattern = new Regex(@"(\d+\.?\d*)bpm");
                                                        var result = matchBpmPattern.Match(ProjectSettings.Config.SoundtrackFilepath);
                                                        if (result.Success)
                                                        {
                                                            float.TryParse(result.Groups[1].Value, out bpm);
                                                            ProjectSettings.Config.SoundtrackBpm = bpm;
                                                            playback.Bpm = bpm;
                                                        }
                                                    }

                                                    if (ImGui.Button("Close"))
                                                    {
                                                        ImGui.CloseCurrentPopup();
                                                    }
                                                }, "Timeline options");
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
                
                if (UserSettings.Config.KeepBeatTimeRunningInPause)
                {
                    var center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax())/2;
                    var beat = (int)(playback.BeatTime * 4) % 4;
                    var bar = (int)(playback.BeatTime) % 4;
                    const int gridSize = 4; 
                    var drawList = ImGui.GetWindowDrawList();
                    var min = center -new Vector2(8,9)  + new Vector2(beat * gridSize, bar*gridSize);
                    drawList.AddRectFilled(min, min + new Vector2(gridSize-1, gridSize-1), Color.Orange);
                }

                ImGui.PopStyleColor();
                ImGui.SameLine();
            }

            // Jump to start
            if (CustomComponents.IconButton(Icon.JumpToRangeStart, "##jumpToBeginning", ControlSize))
            {
                playback.TimeInBars = playback.LoopRange.Start;
            }

            ImGui.SameLine();

            // Prev Keyframe
            if (CustomComponents.IconButton(Icon.JumpToPreviousKeyframe, "##prevKeyframe", ControlSize)
                || KeyboardBinding.Triggered(UserActions.PlaybackJumpToPreviousKeyframe))
            {
                UserActionRegistry.DeferredActions.Add(UserActions.PlaybackJumpToPreviousKeyframe);
            }

            ImGui.SameLine();

            // Play backwards
            var isPlayingBackwards = playback.PlaybackSpeed < 0;
            if (CustomComponents.ToggleButton(Icon.PlayBackwards,
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

            ImGui.SameLine();

            // Play forward
            var isPlaying = playback.PlaybackSpeed > 0;
            if (CustomComponents.ToggleButton(Icon.PlayForwards,
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

            // Stop as separate keyboard 
            if (KeyboardBinding.Triggered(UserActions.PlaybackStop))
            {
                playback.PlaybackSpeed = 0;
            }

            ImGui.SameLine();

            // Next Keyframe
            if (CustomComponents.IconButton(Icon.JumpToNextKeyframe, "##nextKeyframe", ControlSize)
                || KeyboardBinding.Triggered(UserActions.PlaybackJumpToNextKeyframe))
            {
                UserActionRegistry.DeferredActions.Add(UserActions.PlaybackJumpToNextKeyframe);
            }

            ImGui.SameLine();

            // End
            if (CustomComponents.IconButton(Icon.JumpToRangeEnd, "##lastKeyframe", ControlSize))
            {
                playback.TimeInBars = playback.LoopRange.End;
            }

            ImGui.SameLine();

            // Loop
            CustomComponents.ToggleButton(Icon.Loop, "##loop", ref playback.IsLooping, ControlSize);
            ImGui.SameLine();

            // Curve Mode
            if (ImGui.Button(timeLineCanvas.Mode.ToString(), ControlSize))
            {
                timeLineCanvas.Mode = (TimeLineCanvas.Modes)(((int)timeLineCanvas.Mode + 1) % Enum.GetNames(typeof(TimeLineCanvas.Modes)).Length);
            }

            ImGui.SameLine();

            // ToggleAudio
            if (CustomComponents.IconButton(UserSettings.Config.AudioMuted ? Icon.ToggleAudioOff : Icon.ToggleAudioOn, "##audioToggle", ControlSize))
            {
                UserSettings.Config.AudioMuted = !UserSettings.Config.AudioMuted;
                var streamedClipTime = playback as StreamPlayback;
                streamedClipTime?.SetMuteMode(UserSettings.Config.AudioMuted);
            }

            ImGui.SameLine();

            // ToggleHover
            var icon = Icon.HoverPreviewSmall;
            if (UserSettings.Config.HoverMode == GraphCanvas.HoverModes.Disabled)
            {
                icon = Icon.HoverPreviewDisabled;
            }
            else if (UserSettings.Config.HoverMode == GraphCanvas.HoverModes.Live)
            {
                icon = Icon.HoverPreviewPlay;
            }

            if (CustomComponents.IconButton(icon, "##hoverPreview", ControlSize))
            {
                UserSettings.Config.HoverMode =
                    (GraphCanvas.HoverModes)(((int)UserSettings.Config.HoverMode + 1) % Enum.GetNames(typeof(GraphCanvas.HoverModes)).Length);
            }

            ImGui.SameLine();

            // Cut
            if (timeLineCanvas.FoundTimeClipForCurrentTime)
            {
                if (CustomComponents.IconButton(Icon.ConnectedParameter, "##CutClip", ControlSize))
                {
                    var matchingClips = timeLineCanvas.LayersArea.SelectedItems
                                                      .Where(clip => clip.TimeRange.Contains(playback.TimeInBars));

                    var compOp = GraphCanvas.Current.CompositionOp;
                    foreach (var clip in matchingClips)
                    {
                        var compositionSymbolUi = SymbolUiRegistry.Entries[compOp.Symbol.Id];
                        var symbolChildUi = compositionSymbolUi.ChildUis.Single(child => child.Id == clip.Id);

                        Vector2 newPos = symbolChildUi.PosOnCanvas;
                        newPos.Y += symbolChildUi.Size.Y + 5.0f;
                        var cmd = new CopySymbolChildrenCommand(compositionSymbolUi, new[] { symbolChildUi }, compositionSymbolUi, newPos);
                        cmd.Do();

                        // set new end to the original time clip
                        float originalEndTime = clip.TimeRange.End;
                        clip.TimeRange = new TimeRange(clip.TimeRange.Start, (float)playback.TimeInBars);

                        // apply new time range to newly added instance
                        Guid newChildId = cmd.OldToNewIdDict[clip.Id];
                        var newInstance = compOp.Children.Single(child => child.SymbolChildId == newChildId);
                        var newTimeClip = newInstance.Outputs.OfType<ITimeClipProvider>().Single().TimeClip;
                        newTimeClip.TimeRange = new TimeRange((float)playback.TimeInBars, originalEndTime);
                    }
                }
            }
        }

        public static readonly Vector2 ControlSize = new Vector2(45, 26);
    }
}