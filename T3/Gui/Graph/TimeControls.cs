using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using T3.Gui.Animation.CurveEditing;
using T3.Gui.Styling;
using T3.Gui.Windows.TimeLine;
using Icon = T3.Gui.Styling.Icon;

namespace T3.Gui.Graph
{
    internal class TimeControls
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
                
                case ClipTime.TimeModes.Seconds:
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
                                              TimeControlsSize,
                                              trigger: KeyboardBinding.Triggered(UserActions.PlaybackBackwards)))
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
                                              TimeControlsSize,
                                              trigger: KeyboardBinding.Triggered(UserActions.PlaybackToggle)))
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
                var rounded =  Math.Round(clipTime.Time * editFrameRate)/editFrameRate;    
                clipTime.Time = rounded - frameDuration;
            }

            // Step to next frame
            if (KeyboardBinding.Triggered(UserActions.PlaybackNextFrame))
            {
                var rounded =  Math.Round(clipTime.Time * editFrameRate)/editFrameRate;    
                clipTime.Time = rounded + frameDuration;
            }

            // Play backwards with increasing speed
            if (KeyboardBinding.Triggered(UserActions.PlaybackBackwards))
            {
                if (clipTime.PlaybackSpeed >= 0)
                {
                    clipTime.PlaybackSpeed = -1;
                }
                else if (clipTime.PlaybackSpeed > -8)
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
                else if (clipTime.PlaybackSpeed < 8)
                {
                    clipTime.PlaybackSpeed *= 2;
                }
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
            if (CustomComponents.IconButton(Icon.JumpToRangeEnd, "##nlastKeyframe", TimeControlsSize))
            {
                clipTime.Time = clipTime.TimeRangeEnd;
            }
            ImGui.SameLine();

            // Loop
            CustomComponents.ToggleButton(Icon.Loop, "##loop", ref clipTime.IsLooping, TimeControlsSize);
            ImGui.SameLine();
            
            
            if (ImGui.Button(mode.ToString(), TimeControlsSize))
            {
                mode = (TimeLineCanvas.Modes)(((int)mode + 1) % Enum.GetNames(typeof(TimeLineCanvas.Modes)).Length);
            }
            ImGui.SameLine();
        }

        private static readonly Vector2 TimeControlsSize = new Vector2(40, 23);
    }
}