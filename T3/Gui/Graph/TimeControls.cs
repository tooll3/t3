using ImGuiNET;
using System;
using System.Numerics;
using T3.Core.Logging;
using T3.Gui.UiHelpers;
using T3.Gui.Windows.TimeLine;
using Icon = T3.Gui.Styling.Icon;
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace T3.Gui.Graph
{
    internal static class TimeControls
    {
        internal static void DrawTimeControls(ClipTime clipTime, ref TimeLineCanvas.Modes mode)
        {

            // Current Time
            var delta = 0.0;
            string formattedTime = "";
            switch (clipTime.TimeMode)
            {
                case ClipTime.TimeModes.Bars:
                    formattedTime = $"{clipTime.Bar:0}. {clipTime.Beat:0}. {clipTime.Tick:0}.";
                    break;

                case ClipTime.TimeModes.Secs:
                    formattedTime = TimeSpan.FromSeconds(clipTime.Time).ToString(@"hh\:mm\:ss\:ff");
                    break;

                case ClipTime.TimeModes.F30:
                    var frames = clipTime.Time * 30;
                    formattedTime = $"{frames:0}f ";
                    break;
                case ClipTime.TimeModes.F60:
                    var frames60 = clipTime.Time * 60;
                    formattedTime = $"{frames60:0}f ";
                    break;
            }

            if (CustomComponents.JogDial(formattedTime, ref delta, new Vector2(100, 0)))
            {
                clipTime.PlaybackSpeed = 0;
                clipTime.Time += delta;
                if (clipTime.TimeMode == ClipTime.TimeModes.F30)
                {
                    clipTime.Time = Math.Floor(clipTime.Time * 30) / 30;
                }
            }

            ImGui.SameLine();

            if (ImGui.Button(clipTime.TimeMode.ToString(), ControlSize))
            {
                clipTime.TimeMode = (ClipTime.TimeModes)(((int)clipTime.TimeMode + 1) % Enum.GetNames(typeof(ClipTime.TimeModes)).Length);
            }

            CustomComponents.ContextMenuForItem(() =>
                                                {
                                                    var t = (float)clipTime.Bpm;
                                                    ImGui.DragFloat("BPM", ref t);
                                                    clipTime.Bpm = t;
                                                    if (ImGui.Button("Close"))
                                                    {
                                                        ImGui.CloseCurrentPopup();
                                                    }
                                                });

            ImGui.SameLine();

            // Jump to start
            if (CustomComponents.IconButton(Icon.JumpToRangeStart, "##jumpToBeginning", ControlSize))
            {
                clipTime.Time = clipTime.TimeRangeStart;
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
            var isPlayingBackwards = clipTime.PlaybackSpeed < 0;
            if (CustomComponents.ToggleButton(Icon.PlayBackwards,
                                              label: isPlayingBackwards ? $"[{(int)clipTime.PlaybackSpeed}x]" : "<",
                                              ref isPlayingBackwards,
                                              ControlSize))
            {
                if (clipTime.PlaybackSpeed != 0)
                {
                    clipTime.PlaybackSpeed = 0;
                }
                else if (clipTime.PlaybackSpeed == 0)
                {
                    clipTime.PlaybackSpeed = -1;
                }
            }

            ImGui.SameLine();

            // Play forward
            var isPlaying = clipTime.PlaybackSpeed > 0;
            if (CustomComponents.ToggleButton(Icon.PlayForwards,
                                              label: isPlaying ? $"[{(int)clipTime.PlaybackSpeed}x]" : ">",
                                              ref isPlaying,
                                              ControlSize))
            {
                if (Math.Abs(clipTime.PlaybackSpeed) > 0.001f)
                {
                    clipTime.PlaybackSpeed = 0;
                }
                else if (Math.Abs(clipTime.PlaybackSpeed) < 0.001f)
                {
                    clipTime.PlaybackSpeed = 1;
                }
            }

            const float editFrameRate = 30;
            const float frameDuration = 1 / editFrameRate;

            // Step to previous frame
            if (KeyboardBinding.Triggered(UserActions.PlaybackPreviousFrame))
            {
                var rounded = Math.Round(clipTime.Time * editFrameRate) / editFrameRate;
                clipTime.Time = rounded - frameDuration;
            }

            // Step to next frame
            if (KeyboardBinding.Triggered(UserActions.PlaybackNextFrame))
            {
                var rounded = Math.Round(clipTime.Time * editFrameRate) / editFrameRate;
                clipTime.Time = rounded + frameDuration;
            }

            // Play backwards with increasing speed
            if (KeyboardBinding.Triggered(UserActions.PlaybackBackwards))
            {
                Log.Debug("Backwards triggered with speed " + clipTime.PlaybackSpeed);
                if (clipTime.PlaybackSpeed >= 0)
                {
                    clipTime.PlaybackSpeed = -1;
                }
                else if (clipTime.PlaybackSpeed > -16)
                {
                    clipTime.PlaybackSpeed *= 2;
                }
            }

            // Play forward with increasing speed
            if (KeyboardBinding.Triggered(UserActions.PlaybackForward))
            {
                if (clipTime.PlaybackSpeed <= 0)
                {
                    clipTime.PlaybackSpeed = 1;
                }
                else if (clipTime.PlaybackSpeed < 16)    // Bass can't play much faster anyways
                {
                    clipTime.PlaybackSpeed *= 2;
                }
            }
            
            
            if (KeyboardBinding.Triggered(UserActions.PlaybackForwardHalfSpeed))
            {
                if(clipTime.PlaybackSpeed > 0 && clipTime.PlaybackSpeed < 1f)
                    clipTime.PlaybackSpeed *= 0.5f;
                else
                    clipTime.PlaybackSpeed = 0.5f;
            }

            // Stop as separate keyboard 
            if (KeyboardBinding.Triggered(UserActions.PlaybackStop))
            {
                clipTime.PlaybackSpeed = 0;
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
                clipTime.Time = clipTime.TimeRangeEnd;
            }

            ImGui.SameLine();

            // Loop
            CustomComponents.ToggleButton(Icon.Loop, "##loop", ref clipTime.IsLooping, ControlSize);
            ImGui.SameLine();

            // Curve MOde
            if (ImGui.Button(mode.ToString(), ControlSize))
            {
                mode = (TimeLineCanvas.Modes)(((int)mode + 1) % Enum.GetNames(typeof(TimeLineCanvas.Modes)).Length);
            }
            ImGui.SameLine();
            
            // ToggleAudio
            if (CustomComponents.IconButton(UserSettings.Config.AudioMuted ? Icon.ToggleAudioOff : Icon.ToggleAudioOn,  "##audioToggle", ControlSize))
            {
                UserSettings.Config.AudioMuted = !UserSettings.Config.AudioMuted;
                var streamedClipTime = clipTime as StreamClipTime;
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
            if (CustomComponents.IconButton(icon,  "##hoverPreview", ControlSize))
            {
                UserSettings.Config.HoverMode = (GraphCanvas.HoverModes)(((int)UserSettings.Config.HoverMode + 1) % Enum.GetNames(typeof(GraphCanvas.HoverModes)).Length);
            }
            ImGui.SameLine();
        }

        public static readonly Vector2 ControlSize = new Vector2(45, 26);
    }
}