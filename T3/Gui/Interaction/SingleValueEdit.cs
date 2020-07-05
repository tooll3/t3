using System;
using System.Data;
using ImGuiNET;
using T3.Core;
using T3.Core.Logging;
using T3.Gui.InputUi;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;
using Vector2 = System.Numerics.Vector2;

namespace T3.Gui.Interaction
{
    /// <summary>
    /// An alternative ImGui component for editing float values 
    /// </summary>
    public static class SingleValueEdit
    {
        /// <summary>
        /// Wrapper coll for int type 
        /// </summary>
        public static InputEditStateFlags Draw(ref int value,
                                               Vector2 size,
                                               int min = int.MinValue,
                                               int max = int.MaxValue,
                                               bool clamp= false,
                                               float scale = 0.1f,
                                               string format = "{0:0}"
                                               )
        {
            double doubleValue = value;
            var result = Draw(ref doubleValue, size, min, max,clamp, scale, format);
            value = (int)doubleValue;
            return result;
        }

        /// <summary>
        /// Wrapper call for float type
        /// </summary>
        public static InputEditStateFlags Draw(ref float value,
                                               Vector2 size,
                                               float min = float.NegativeInfinity,
                                               float max = float.PositiveInfinity,
                                               bool clamp= false,
                                               float scale = 0.01f,
                                               string format = "{0:0.000}"
                                                )
        {
            double floatValue = value;
            var result = Draw(ref floatValue, size, min, max, clamp, scale, format);
            value = (float)floatValue;
            return result;
        }

        public static InputEditStateFlags Draw(ref double value,
                                               Vector2 size,
                                               double min = double.NegativeInfinity,
                                               double max = double.PositiveInfinity,
                                               bool clamp = false,
                                               float scale = 1,
                                               string format = "{0:0.000}"
                                               )
        {
            // Automatically set scale from range
            if (scale <= 0)
            {
                // A lame hack because we can't set infinity values in parameter properties
                if (min < -9999 || max > 9999)
                {
                    scale = 0.01f;
                }
                else
                {
                    scale = (float)Math.Abs(min - max) / 100;
                }
            }
            
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
                        DrawValueRangeIndicator(value, min, max);
                        ImGui.PopStyleColor(3);

                        if (ImGui.IsMouseReleased(0))
                        {
                            var wasClick = ImGui.GetIO().MouseDragMaxDistanceSqr[0] < 4;
                            if (wasClick)
                            {
                                if (io.KeyCtrl)
                                {
                                    SetState(JogDialStates.Inactive);
                                    return InputEditStateFlags.ResetToDefault;
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

                        if (UserSettings.Config.UseJogDialControl)
                        {
                            JogDialOverlay.Draw(io, min, max, scale, clamp);
                        }
                        else
                        {
                            SliderLadder.Draw(io, min, max, scale, (float)(ImGui.GetTime() - _timeOpened), clamp);                            
                        }

                        break;

                    case JogDialStates.StartedTextInput:
                        ImGui.SetKeyboardFocusHere();
                        SetState(JogDialStates.TextInput);
                        goto case JogDialStates.TextInput;

                    case JogDialStates.TextInput:
                        ImGui.PushStyleColor(ImGuiCol.Text, double.IsNaN(_editValue)
                                                                ? Color.Red.Rgba
                                                                : Color.White.Rgba);
                        ImGui.SetNextItemWidth(size.X);
                        ImGui.InputText("##dialInput", ref _jogDialText, 20);
                        ImGui.PopStyleColor();

                        if (ImGui.IsItemDeactivated())
                        {
                            SetState(JogDialStates.Inactive);
                            ImGui.SetKeyboardFocusHere();    // Clear focus so next time value will be completely selected
                            if (double.IsNaN(_editValue))
                                _editValue = _startValue;
                        }

                        _editValue = Evaluate(_jogDialText);
                        break;
                }

                value = _editValue;
                if (_state == JogDialStates.Inactive)
                {
                    return InputEditStateFlags.Finished;
                }

                //_editValue = Math.Round(_editValue * 100) / 100;
                return Math.Abs(_editValue - _startValue) > 0.0001f ? InputEditStateFlags.Modified : InputEditStateFlags.Started;
            }

            DrawButtonWithDynamicLabel(FormatValueForButton(ref value), ref size);
            
            DrawValueRangeIndicator(value, min, max);
            
            if (ImGui.IsItemActivated())
            {
                _activeJogDialId = id;
                _editValue = value;
                _startValue = value;
                _jogDialText = FormatValueForButton(ref value);
                SetState(JogDialStates.Dialing);
            }

            return InputEditStateFlags.Nothing;
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
                    _timeOpened = ImGui.GetTime();
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
            // Don't use rounding for integers
            return (_numberFormat == "{0:0}")
                       ? "" + (int)value
                       : string.Format(_numberFormat, value);    
            
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

        
        private static void DrawValueRangeIndicator(double value, double min, double max)
        {
            if (!double.IsInfinity(min) || !double.IsInfinity(max))
            {
                var itemSize = ImGui.GetItemRectSize();
                var itemPos = ImGui.GetItemRectMin();

                var center = 0.0;
                if (min < 0)
                {
                    center = MathUtils.Remap((min + max) * 0.5, min, max, 0, itemSize.X);
                }

                var end = MathUtils.Remap(value, min, max, 0, itemSize.X);
                var orgCenter = center;
                
                if (center > end)
                {
                    var t = center;
                    center = end;
                    end = t;
                }

                var p1 = itemPos + new Vector2((float)center, 0);
                var p2 = itemPos + new Vector2((float)end, itemSize.Y);
                ImGui.GetWindowDrawList().AddRectFilled(p1, p2, ValueIndicatorColor);
                
                // Indicate center
                var alignment = center < orgCenter ? -1 : 0;
                ImGui.GetWindowDrawList().AddRectFilled(
                                                        ImGui.GetItemRectMin() + new Vector2((float)orgCenter + alignment, 0), 
                                                        ImGui.GetItemRectMin() + new Vector2((float)orgCenter+ alignment+1, itemSize.Y), 
                                                        ValueIndicatorColor);
                
                // Indicate overflow
                if (value < min)
                {
                    var triangleCenter = new Vector2(itemPos.X + 5, itemPos.Y + itemSize.Y - 5);
                    ImGui.GetWindowDrawList().AddTriangleFilled( 
                                                                triangleCenter + new Vector2(-3, 0),
                                                                triangleCenter + new Vector2(2, -4),
                                                                triangleCenter + new Vector2(2, 4),
                                                                new Color(1,1,1,0.2f)
                                                                );
                }
                else if (value > max)
                {
                    var triangleCenter = new Vector2(itemPos.X + itemSize.X - 3, itemPos.Y + itemSize.Y - 5);
                    ImGui.GetWindowDrawList().AddTriangleFilled( 
                                                                triangleCenter + new Vector2(-2, -4),
                                                                triangleCenter + new Vector2(3, 0),
                                                                triangleCenter + new Vector2(-2, 4),
                                                                new Color(1,1,1,0.2f)
                                                               );                    
                }
            }
        }
        private static readonly Color ValueIndicatorColor = new Color(1,1,1,0.06f); 

        
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

        private static string _numberFormat = "{0:0.000}";
        private static double _timeOpened;

        /// <summary>
        /// Draws a range of virtual slider overlays
        /// </summary>
        private static class SliderLadder
        {
            private struct RangeDef
            {
                public readonly float YMin;
                public readonly float YMax;
                public readonly float ScaleFactor;
                public readonly string Label;
                public readonly float FadeInDelay;

                public RangeDef(float yMin, float yMax, float scaleFactor, string label, float fadeInDelay)
                {
                    YMin = yMin;
                    YMax = yMax;
                    ScaleFactor = scaleFactor;
                    Label = label;
                    FadeInDelay = fadeInDelay;
                }
            }

            private static readonly RangeDef[] Ranges =
            {
                new RangeDef(-4f * OuterRangeHeight, -3f * OuterRangeHeight, 1000, "x1000", 2),
                new RangeDef(-3f * OuterRangeHeight, -2f * OuterRangeHeight, 100, "x100", 1),
                new RangeDef(-2f * OuterRangeHeight, -1f * OuterRangeHeight, 10, "x10", 0),
                new RangeDef(-1f * OuterRangeHeight, 1f * OuterRangeHeight, 1, "", 0),
                new RangeDef(1f * OuterRangeHeight, 2f * OuterRangeHeight, 0.1f, "x0.1", 0),
                new RangeDef(2f * OuterRangeHeight, 3f * OuterRangeHeight, 0.01f, "x0.01", 1),
            };

            public static void Draw(ImGuiIOPtr io, double min, double max, float scale, float timeSinceVisible, bool clamp)
            {
                var foreground = ImGui.GetForegroundDrawList();
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                var pLast = io.MousePos - io.MouseDelta - _center;
                var pNow = io.MousePos - _center;
                if (timeSinceVisible < 0.032f)
                {
                    _lastStepPosX = pNow.X;
                }

                float activeScaleFactor = 0;

                foreach (var range in Ranges)
                {
                    var isActiveRange = pNow.Y > range.YMin && pNow.Y < range.YMax;
                    var opacity = (timeSinceVisible * 4 - range.FadeInDelay / 4).Clamp(0, 1);

                    var isCenterRange = Math.Abs(range.ScaleFactor - 1) < 0.001f;
                    if (isActiveRange)
                    {
                        activeScaleFactor = range.ScaleFactor;
                    }
                    
                    if (!isCenterRange)
                    {
                        var centerColor = (isActiveRange ? RangeActiveColor : RangeCenterColor)*opacity;
                        
                        foreground.AddRectFilledMultiColor(
                                                           new Vector2(-RangeWidth, range.YMin) + _center,
                                                           new Vector2(0, range.YMax - RangePadding) + _center,
                                                           RangeOuterColor,
                                                           centerColor,
                                                           centerColor,
                                                           RangeOuterColor
                                                          );

                        foreground.AddRectFilledMultiColor(
                                                           new Vector2(0, range.YMin) + _center,
                                                           new Vector2(RangeWidth, range.YMax - RangePadding) + _center,
                                                           centerColor,
                                                           RangeOuterColor,
                                                           RangeOuterColor,
                                                           centerColor
                                                          );
                        foreground.AddText(Fonts.FontLarge,
                                           Fonts.FontLarge.FontSize,
                                           new Vector2(-20, range.YMin) + _center,
                                           isActiveRange ? (Color.White * opacity) : Color.Black,
                                           range.Label);
                    }
                }

                var deltaSinceLastStep = pLast.X - _lastStepPosX;
                var delta = deltaSinceLastStep / StepSize;
                if (io.KeyAlt)
                {
                    ImGui.PushFont(Fonts.FontSmall);
                    foreground.AddText(ImGui.GetMousePos() + new Vector2(10, 10), Color.Gray, "x0.01");
                    ImGui.PopFont();
                    
                    delta *= 0.01f;
                }
                else if (io.KeyShift)
                {
                    ImGui.PushFont(Fonts.FontSmall);
                    foreground.AddText(ImGui.GetMousePos() + new Vector2(10, 10), Color.Gray, "x10");
                    ImGui.PopFont();

                    delta *= 10f;
                }
                
                if (!(Math.Abs(deltaSinceLastStep) >= StepSize))
                    return;
                
                _editValue += delta * activeScaleFactor * scale;
                if (activeScaleFactor > 1)
                {
                    _editValue = Math.Round(_editValue / (activeScaleFactor * scale)) * (activeScaleFactor * scale);
                }

                if(clamp)
                    _editValue = _editValue.Clamp(min, max);
                
                _lastStepPosX = pNow.X;
            }

            private const float StepSize = 3;
            private static float _lastStepPosX;
            private const float RangeWidth = 300;
            private const float OuterRangeHeight = 50;
            private const float RangePadding = 1;
            private static readonly Color RangeCenterColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            private static readonly Color RangeOuterColor = new Color(0.3f, 0.3f, 0.3f, 0.0f);
            private static readonly Color RangeActiveColor = new Color(0.0f, 0.0f, 0.0f, 0.7f);
        }

        /// <summary>
        /// Draws a circular dial to manipulate values with various speeds
        /// </summary>
        private static class JogDialOverlay
        {
            public static void Draw(ImGuiIOPtr io, double min, double max, float scale, bool clamp)
            {
                var foreground = ImGui.GetForegroundDrawList();
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                var pLast = io.MousePos - io.MouseDelta - _center;
                var pNow = io.MousePos - _center;

                var distanceToCenter = pNow.Length();

                var r = NeutralRadius;
                float activeSpeed = 0;
                int index = 0;
                var rot = (float)MathUtils.Fmod(((_editValue - _startValue) * RadialIndicatorSpeed), 2 * Math.PI);
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

                delta = (float)Math.Round(delta * 50) / 50;
                if (ImGui.GetIO().KeyAlt)
                {
                    activeSpeed *= 0.01f;
                }
                else if (ImGui.GetIO().KeyShift)
                {
                    activeSpeed *= 10;
                }

                _editValue += delta * activeSpeed * scale * 100;
                
                if(clamp)
                    _editValue = _editValue.Clamp(min, max);
            }

            private const float SegmentWidth = 90;
            private const float NeutralRadius = 10;
            private const float RadialIndicatorSpeed = (float)(2 * Math.PI / 20);
            private const float Padding = 2;

            private static readonly float[] SegmentSpeeds = new[]
                                                   {
                                                       (float)(0.5f / Math.PI),
                                                       (float)(20 * 0.5f / Math.PI)
                                                   };

            private static readonly Color SegmentColor = new Color(1f, 1f, 1f, 0.05f);
            private static readonly Color ActiveSegmentColor = new Color(1f, 1f, 1f, 0.05f);
        }
    }
}