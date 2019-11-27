using ImGuiNET;
using System;
using System.Numerics;
using T3.Core.Logging;
using T3.Gui.Windows.TimeLine;
using Icon = T3.Gui.Styling.Icon;
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace T3.Gui.Graph
{
    internal static class TimeControls
    {
        internal static void DrawTimeControls(ClipTime clipTime, ref TimeLineCanvas.Modes mode)
        {
            ImGui.SetCursorPos(
                               new Vector2(
                                           ImGui.GetWindowContentRegionMin().X,
                                           ImGui.GetWindowContentRegionMax().Y - TimeControlsSize.Y));

            ImGui.Button("##Timeline", TimeControlsSize);
            ImGui.SameLine();

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

            if (CustomComponents.JogDial(formattedTime, ref delta, new Vector2(80, 0)))
            {
                clipTime.PlaybackSpeed = 0;
                clipTime.Time += delta;
                if (clipTime.TimeMode == ClipTime.TimeModes.F30)
                {
                    clipTime.Time = Math.Floor(clipTime.Time * 30) / 30;
                }
            }

            ImGui.SameLine();

            if (ImGui.Button(clipTime.TimeMode.ToString(), TimeControlsSize))
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
            if (CustomComponents.IconButton(Icon.JumpToRangeStart, "##jumpToBeginning", TimeControlsSize))
            {
                clipTime.Time = clipTime.TimeRangeStart;
            }

            ImGui.SameLine();

            // Prev Keyframe
            if (CustomComponents.IconButton(Icon.JumpToPreviousKeyframe, "##prevKeyframe", TimeControlsSize)
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
                                              TimeControlsSize))
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
                                              TimeControlsSize))
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
            if (CustomComponents.IconButton(Icon.JumpToNextKeyframe, "##nextKeyframe", TimeControlsSize)
                || KeyboardBinding.Triggered(UserActions.PlaybackJumpToNextKeyframe))
            {
                UserActionRegistry.DeferredActions.Add(UserActions.PlaybackJumpToNextKeyframe);
            }

            ImGui.SameLine();

            // End
            if (CustomComponents.IconButton(Icon.JumpToRangeEnd, "##lastKeyframe", TimeControlsSize))
            {
                clipTime.Time = clipTime.TimeRangeEnd;
            }

            ImGui.SameLine();

            // Loop
            CustomComponents.ToggleButton(Icon.Loop, "##loop", ref clipTime.IsLooping, TimeControlsSize);
            ImGui.SameLine();

            // Curve MOde
            if (ImGui.Button(mode.ToString(), TimeControlsSize))
            {
                mode = (TimeLineCanvas.Modes)(((int)mode + 1) % Enum.GetNames(typeof(TimeLineCanvas.Modes)).Length);
            }
            ImGui.SameLine();
            
            // ToggleAudio
            if (CustomComponents.IconButton(clipTime.AudioMuted ? Icon.ToggleAudioOff : Icon.ToggleAudioOn,  "##audioToggle", TimeControlsSize))
            {
                clipTime.AudioMuted = !clipTime.AudioMuted;
                var streamedClipTime = clipTime as StreamClipTime;
                streamedClipTime?.SetMuteMode(clipTime.AudioMuted);
            }
            ImGui.SameLine();
            
            // ToggleHover
            var icon = Icon.HoverPreviewSmall;
            if (GraphCanvas.HoverMode == GraphCanvas.HoverModes.Disabled)
            {
                icon = Icon.HoverPreviewDisabled;
            }
            else if (GraphCanvas.HoverMode == GraphCanvas.HoverModes.Live)
            {
                icon = Icon.HoverPreviewPlay;
            }
            if (CustomComponents.IconButton(icon,  "##hoverPreview", TimeControlsSize))
            {
                GraphCanvas.HoverMode = (GraphCanvas.HoverModes)(((int)GraphCanvas.HoverMode + 1) % Enum.GetNames(typeof(GraphCanvas.HoverModes)).Length);
            }
            ImGui.SameLine();
        }

        private static readonly Vector2 TimeControlsSize = new Vector2(40, 23);
    }
}