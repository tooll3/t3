using System;
using System.Data;
using System.Numerics;
using ImGuiNET;
using T3.Gui.InputUi;
using UiHelpers;

namespace T3.Gui.Interaction
{
    public static class SingleValueEdit
    {
        public static InputEditState Draw(ref int value, Vector2 size, int min = int.MinValue,
                                          int max = int.MaxValue, float scale = 0.1f, string format = "{0:0}")
        {
            double doubleValue = value;
            var result = Draw(ref doubleValue, size, min, max, scale, format);
            value = (int)doubleValue;
            return result;
        }

        public static InputEditState Draw(ref float value, Vector2 size, float min = float.NegativeInfinity,
                                          float max = float.PositiveInfinity, float scale = 0.01f, string format = "{0:0.00}")
        {
            double floatValue = value;
            var result = Draw(ref floatValue, size, min, max, scale, format);
            value = (float)floatValue;
            return result;
        }

        /// <summary>
        /// Returns true if editing was completed and value changed
        /// </summary>
        public static InputEditState Draw(ref double value, Vector2 size, double min = double.NegativeInfinity,
                                          double max = double.PositiveInfinity, float scale = 1, string format = "{0:0.00}")
        {
            var io = ImGui.GetIO();
            var id = ImGui.GetID("jog");
            _numberFormat = format;
            if (id == _activeJogDialId)
            {
                switch (_state)
                {
                    case JogDialStates.Dialing:
                        ImGui.PushStyleColor(ImGuiCol.Button, Color.Black.Rgba);
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Color.Black.Rgba);
                        ImGui.PushStyleColor(ImGuiCol.ButtonActive, Color.Black.Rgba);
                        DrawButtonWithDynamicLabel(FormatValueForButton(ref _editValue), ref size);
                        ImGui.PopStyleColor(3);

                        if (ImGui.IsMouseReleased(0))
                        {
                            var wasClick = ImGui.GetIO().MouseDragMaxDistanceSqr[0] < 4;
                            if (wasClick)
                            {
                                if (io.KeyCtrl)
                                {
                                    SetState(JogDialStates.Inactive);
                                    return InputEditState.ResetToDefault;
                                }
                                else
                                {
                                    SetState(JogDialStates.StartedTextInput);
                                }
                            }
                            else
                            {
                                SetState(JogDialStates.Inactive);
                            }

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
                        var rot = (float)Im.Fmod(((_editValue - _startValue) * RadialIndicatorSpeed), 2 * Math.PI);
                        foreach (var segmentSpeed in SegmentSpeeds)
                        {
                            var isLastSegment = index == SegmentSpeeds.Length - 1;
                            var isActive =
                                (distanceToCenter > r && distanceToCenter < r + SegmentWidth) ||
                                (isLastSegment && distanceToCenter > r + SegmentWidth);

                            if (isActive)
                                activeSpeed = segmentSpeed;

                            if (isActive)
                            {
                                const float opening = 3.14f * 1.75f;

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

                        delta = (float)Math.Round(delta*50)/50;

                        _editValue += delta * activeSpeed * scale * 100;
                        _editValue = _editValue.Clamp(min, max);
                        break;

                    case JogDialStates.StartedTextInput:
                        ImGui.SetKeyboardFocusHere();
                        SetState(JogDialStates.TextInput);
                        goto case JogDialStates.TextInput;

                    case JogDialStates.TextInput:
                        ImGui.PushStyleColor(ImGuiCol.Text, double.IsNaN(_editValue)
                                                                ? Color.Red.Rgba
                                                                : Color.White.Rgba);
                        ImGui.InputText("##dialInput", ref _jogDialText, 20);
                        ImGui.PopStyleColor();

                        if (ImGui.IsItemDeactivated())
                        {
                            SetState(JogDialStates.Inactive);
                            if (double.IsNaN(_editValue))
                                _editValue = _startValue;
                        }

                        _editValue = Evaluate(_jogDialText);
                        break;
                }

                value = _editValue;
                if (_state == JogDialStates.Inactive)
                {
                    return InputEditState.Finished;
                }

                _editValue = Math.Round(_editValue * 100)/ 100;
                return Math.Abs(_editValue - _startValue) > 0.0001f ? InputEditState.Modified : InputEditState.Started;
            }

            DrawButtonWithDynamicLabel(FormatValueForButton(ref value), ref size);
            if (ImGui.IsItemActivated())
            {
                _activeJogDialId = id;
                _editValue = value;
                _startValue = value;
                _jogDialText = FormatValueForButton(ref value);
                SetState(JogDialStates.Dialing);
            }

            return InputEditState.Nothing;
        }

        private static void SetState(JogDialStates newState)
        {
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

        private static double Evaluate(string expression)
        {
            try
            {
                var table = new DataTable();
                table.Columns.Add("expression", typeof(string), expression);
                var row = table.NewRow();
                table.Rows.Add(row);
                return double.Parse((string)row["expression"]);
            }
            catch
            {
                return float.NaN;
            }
        }

        private static string FormatValueForButton(ref double value)
        {
            return string.Format(_numberFormat, value);
        }

        /// <summary>
        /// A horrible ImGui work around to have button that stays active while its label changes.  
        /// </summary>
        private static void DrawButtonWithDynamicLabel(string label, ref Vector2 size)
        {
            var color1 = Color.GetStyleColor(ImGuiCol.Text);
            var keepPos = ImGui.GetCursorScreenPos();
            ImGui.Button("##dial", size);
            ImGui.GetWindowDrawList().AddText(keepPos + new Vector2(4, 4), color1, label);
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
        private static double _editValue;
        private static double _startValue;
        private static string _jogDialText = "";
        private static JogDialStates _state = JogDialStates.Inactive;
        private const float SegmentWidth = 90;
        private const float NeutralRadius = 10;
        private const float RadialIndicatorSpeed = (float)(2 * Math.PI / 20);
        private const float Padding = 2;

        private static string _numberFormat = "{0:0.00}";

        private static readonly float[] SegmentSpeeds = new[]
                                                        {
                                                            (float)(0.5f / Math.PI),
                                                            (float)(20 * 0.5f / Math.PI)
                                                        };

        private static readonly Color SegmentColor = new Color(1f, 1f, 1f, 0.05f);
        private static readonly Color ActiveSegmentColor = new Color(1f, 1f, 1f, 0.05f);
    }
}