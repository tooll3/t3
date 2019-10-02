using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using T3.Gui.Animation.CurveEditing;

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

            TimeSpan timespan = TimeSpan.FromSeconds(clipTime.Time);

            var delta = 0.0;
            if (CustomComponents.JogDial(timespan.ToString(@"hh\:mm\:ss\:ff"), ref delta, new Vector2(80, 0)))
            {
                clipTime.PlaybackSpeed = 0;
                clipTime.Time += delta;
            }

            ImGui.SameLine();
            ImGui.Button("[<", _timeControlsSize);
            ImGui.SameLine();
            ImGui.Button("<<", _timeControlsSize);
            ImGui.SameLine();

            var isPlayingBackwards = clipTime.PlaybackSpeed < 0;
            if (CustomComponents.ToggleButton(
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
            if (CustomComponents.ToggleButton(
                    label: isPlaying ? $"[{(int)clipTime.PlaybackSpeed}x]" : ">",
                    ref isPlaying,
                    _timeControlsSize,
                    trigger: KeyboardBinding.Triggered(UserActions.PlaybackToggle)))
            {
                if (clipTime.PlaybackSpeed != 0)
                {
                    clipTime.PlaybackSpeed = 0;
                }
                else if (clipTime.PlaybackSpeed == 0)
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
            ImGui.Button(">>", _timeControlsSize);
            ImGui.SameLine();
            ImGui.Button(">]", _timeControlsSize);
            ImGui.SameLine();
            CustomComponents.ToggleButton("Loop", ref clipTime.IsLooping, _timeControlsSize);
            ImGui.SameLine();

            if (ImGui.Button("Key"))
            {
                curveEditor.ToggleKeyframes();
            }
            ImGui.SameLine();
        }

        public static Vector2 _timeControlsSize = new Vector2(40, 0);
    }
}
