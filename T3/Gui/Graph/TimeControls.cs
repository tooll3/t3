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
using Icon = T3.Gui.Styling.Icon;

namespace T3.Gui.Graph
{
    internal class TimeControls
    {
        internal static void DrawTimeControls(ClipTime clipTime, CurveEditCanvas curveEditor)
        {
            ImGui.SetCursorPos(
                               new Vector2(
                                           ImGui.GetWindowContentRegionMin().X,
                                           ImGui.GetWindowContentRegionMax().Y - 30));

            var timespan = TimeSpan.FromSeconds(clipTime.Time);

            var delta = 0.0;
            if (CustomComponents.JogDial(timespan.ToString(@"hh\:mm\:ss\:ff"), ref delta, new Vector2(80, 0)))
            {
                clipTime.PlaybackSpeed = 0;
                clipTime.Time += delta;
            }

            ImGui.SameLine();

            CustomComponents.IconButton(Icon.JumpToFirstKeyframe, "##jumpToBeginning", _timeControlsSize);
            ImGui.SameLine();

            //CustomComponents.IconButton(Icon.JumpToFirstKeyframe,"##firstKeyframe",  _timeControlsSize);
            //ImGui.SameLine();

            CustomComponents.IconButton(Icon.JumpToPreviousKeyframe, "##prevKeyframe", _timeControlsSize);
            ImGui.SameLine();

            var isPlayingBackwards = clipTime.PlaybackSpeed < 0;
            if (CustomComponents.ToggleButton(Icon.PlayBackwards,
                                              label: isPlayingBackwards ? $"[{(int)clipTime.PlaybackSpeed}x]" : "<",
                                              ref isPlayingBackwards,
                                              _timeControlsSize))
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
                                              _timeControlsSize,
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

            if (KeyboardBinding.Triggered(UserActions.PlaybackStop))
            {
                clipTime.PlaybackSpeed = 0;
            }

            ImGui.SameLine();

            CustomComponents.IconButton(Icon.JumpToNextKeyframe, "##nextKeyframe", _timeControlsSize);
            ImGui.SameLine();

            CustomComponents.IconButton(Icon.JumpToLastKeyframe, "##nlastKeyframe", _timeControlsSize);
            ImGui.SameLine();

            CustomComponents.ToggleButton(Icon.Loop, "##loop", ref clipTime.IsLooping, _timeControlsSize);
            ImGui.SameLine();

            if (curveEditor != null)
            {
                if (ImGui.Button("Key"))
                {
                    curveEditor.ToggleKeyframes();
                }

                ImGui.SameLine();
            }
        }

        private static readonly Vector2 _timeControlsSize = new Vector2(40, 23);
    }
}