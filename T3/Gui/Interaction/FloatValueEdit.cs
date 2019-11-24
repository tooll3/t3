using System;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;

namespace T3.Gui.Interaction
{
    public static class FloatValueEdit
    {
        /// <summary>
        /// Returns true if editing was completed and value changed
        /// </summary>
        public static bool Draw(ref float value, Vector2 size)
        {
            var io = ImGui.GetIO();
            var id = ImGui.GetID("jog");
            if (id == _activeJogDialId)
            {
                switch (_state)
                {
                    case JogDialStates.Dialing:
                        ImGui.PushStyleColor(ImGuiCol.Button, Color.Black.Rgba);
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Color.Black.Rgba);
                        ImGui.PushStyleColor(ImGuiCol.ButtonActive, Color.Black.Rgba);
                        DrawButtonWithDynamicLabel(FormatFloatForButton(ref _editValue), ref size);
                        ImGui.PopStyleColor(3);

                        if (ImGui.IsMouseReleased(0))
                        {
                            SetState(ImGui.GetIO().MouseDragMaxDistanceSqr[0] < 10
                                         ? JogDialStates.StartedTextInput
                                         : JogDialStates.Inactive);
                            break;
                        }

                        if (ImGui.IsItemDeactivated())
                        {
                            SetState(JogDialStates.Inactive);
                            break;
                        }

                        var foreground = ImGui.GetForegroundDrawList();
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                        var pLast = io.MousePos - io.MouseDelta - _center;
                        var pNow = io.MousePos - _center;

                        var distanceToCenter = pNow.Length();

                        var r = NeutralRadius;
                        float activeSpeed = 0;
                        int index = 0;
                        foreach (var segmentSpeed in SegmentSpeeds)
                        {
                            var isLastSegment = index == SegmentSpeeds.Length -1;
                            var isActive = 
                                           (distanceToCenter > r && distanceToCenter < r + SegmentWidth) || 
                                            (isLastSegment && distanceToCenter > r + SegmentWidth);
                            
                            if (isActive)
                                activeSpeed = segmentSpeed;

                            if (isActive)
                            {
                                const float opening = 3.14f * 1.75f;
                                var rot = (_editValue - value) * RadialIndicatorSpeed;
                                foreground.PathArcTo(
                                                     _center,
                                                     radius: r + SegmentWidth / 2,
                                                     rot,
                                                     rot + opening,
                                                     num_segments: 64);
                                foreground.PathStroke(ActiveSegmentColor, false, SegmentWidth - Padding);
                            }
                            else
                            {
                                foreground.AddCircle(_center,
                                                     radius: r + SegmentWidth / 2,
                                                     SegmentColor,
                                                     num_segments: 64,
                                                     thickness: SegmentWidth - Padding);
                            }

                            r += SegmentWidth;
                            index++;
                        }

                        var aLast = (float)Math.Atan2(pLast.X, pLast.Y);
                        var aNow = (float)Math.Atan2(pNow.X, pNow.Y);
                        var delta = aLast - aNow;
                        if (delta > 1.5f)
                        {
                            delta -= (float)(2 * Math.PI);
                        }
                        else if (delta < -1.5f)
                        {
                            delta += (float)(2 * Math.PI);
                        }

                        _editValue += delta * activeSpeed;
                        break;

                    case JogDialStates.StartedTextInput:
                        ImGui.SetKeyboardFocusHere();
                        SetState(JogDialStates.TextInput);
                        goto case JogDialStates.TextInput;

                    case JogDialStates.TextInput:
                        ImGui.InputText("##dialInput", ref _jogDialText, 10);
                        if (ImGui.IsItemDeactivated())
                        {
                            SetState(JogDialStates.Inactive);
                        }

                        break;
                }

                value = _editValue;
                if (_state == JogDialStates.Inactive)
                {
                    return true;
                }
            }
            else
            {
                DrawButtonWithDynamicLabel(FormatFloatForButton(ref value), ref size);
                if (ImGui.IsItemActivated())
                {
                    _activeJogDialId = id;
                    _editValue = value;
                    SetState(JogDialStates.Dialing);
                }
            }
            return false;
        }

        private static void SetState(JogDialStates newState)
        {
            Log.Debug($" {_state} -> {newState}");
            switch (newState)
            {
                case JogDialStates.Inactive:
                {
                    _activeJogDialId = 0;
                    break;
                }

                case JogDialStates.Dialing:
                    _center = ImGui.GetMousePos();
                    break;

                case JogDialStates.StartedTextInput:
                    break;

                case JogDialStates.TextInput:
                    break;
            }

            _state = newState;
        }
        
        private static string FormatFloatForButton(ref float value, string format = "{0:0.00}")
        {
            return string.Format(format, value);
        }
        
        
        private static bool DrawButtonWithDynamicLabel(string label, ref Vector2 size)
        {
            unsafe
            {
                var keepPos = ImGui.GetCursorScreenPos();
                var result = ImGui.Button("##dial", size);
                ImGui.GetWindowDrawList().AddText(keepPos + new Vector2(4,4), Color.Gray, label);
                return result;
            }
        }

        
        private enum JogDialStates
        {
            Inactive,
            Dialing,
            StartedTextInput,
            TextInput,
        }

        
        private static uint _activeJogDialId;
        private static Vector2 _center;
        private static float _editValue;
        private static float _startValue;
        private static string _jogDialText = "";
        private static JogDialStates _state = JogDialStates.Inactive;
        private const float SegmentWidth = 90;
        private const float NeutralRadius = 10;
        private const float RadialIndicatorSpeed = (float)(2*Math.PI/20);
        private const float Padding = 2;
        private static readonly float[] SegmentSpeeds = new[]
                                                       {
                                                           (float)(0.5f/Math.PI), 
                                                           (float)(20*0.5f/Math.PI)
                                                       };
        private static readonly Color SegmentColor = new Color(0.1f, 0.1f, 0.1f, 0.2f);
        private static readonly Color ActiveSegmentColor = new Color(0.1f, 0.1f, 0.1f, 0.3f);
    }
}