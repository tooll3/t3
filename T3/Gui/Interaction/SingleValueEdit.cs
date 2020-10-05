using System;
using System.Data;
using System.Globalization;
using ImGuiNET;
using SharpDX;
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
                                               bool clamp = false,
                                               float scale = 0.1f,
                                               string format = "{0:0}")
        {
            double doubleValue = value;
            var result = Draw(ref doubleValue, size, min, max, clamp, scale, format);
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
                                               bool clamp = false,
                                               float scale = 0.01f,
                                               string format = "{0:0.000}")
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
                                               string format = "{0:0.000}")
        {
            CurrentTabIndex++;
            var id = ImGui.GetID("jog");

            var shouldFocus = CurrentTabIndex == TabFocusIndex;
            if (shouldFocus)
            {
                
                //Log.Debug("  ShouldFocus for index " + TabFocusIndex  +  "  state " + _state );
                SetState(InputStates.TextInput);
                _activeJogDialId = id;
                _jogDialText = FormatValueForButton(ref value);
            }
            
            var io = ImGui.GetIO();

            _numberFormat = format;
            if (id == _activeJogDialId)
            {
                switch (_state)
                {
                    case InputStates.Dialing:
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
                                    SetState(InputStates.Inactive);
                                    return InputEditStateFlags.ResetToDefault;
                                }
                                else
                                {
                                    SetState(InputStates.StartedTextInput);
                                }
                            }
                            else
                            {
                                SetState(InputStates.Inactive);
                            }

                            break;
                        }

                        if (ImGui.IsItemDeactivated())
                        {
                            SetState(InputStates.Inactive);
                            break;
                        }

                        if (UserSettings.Config.UseJogDialControl)
                        {
                            JogDialOverlay.Draw(ref _editValue, (float)(ImGui.GetTime() - _timeOpened) < 0.03f,  _center, min, max, scale, clamp);
                        }
                        else
                        {
                            SliderLadder.Draw(io, min, max, scale, (float)(ImGui.GetTime() - _timeOpened), clamp);
                        }

                        break;

                    case InputStates.StartedTextInput:
                        ImGui.SetKeyboardFocusHere();
                        SetState(InputStates.TextInput);
                        goto case InputStates.TextInput;

                    case InputStates.TextInput:
                        ImGui.PushStyleColor(ImGuiCol.Text, double.IsNaN(_editValue)
                                                                ? Color.Red.Rgba
                                                                : Color.White.Rgba);
                        ImGui.SetNextItemWidth(size.X);
                        ImGui.InputText("##dialInput", ref _jogDialText, 20);

                        // Keep Focusing until Tab-Key released
                        if (shouldFocus)
                        {
                            ImGui.SetKeyboardFocusHere(-1);
                            if (!ImGui.IsItemFocused())
                            {
                                if (ImGui.IsKeyReleased((int)Key.Tab))
                                {
                                    TabFocusIndex = -1;
                                }
                            }
                        }

                        ImGui.PopStyleColor();
                        if (ImGui.IsKeyPressed((int)Key.Tab) && TabFocusIndex == -1)
                        {
                            TabFocusIndex = CurrentTabIndex + (ImGui.GetIO().KeyShift ? -1 : 1);
                        }

                        var cancelInputAfterFocusLoss = !shouldFocus && !ImGui.IsItemActive();
                        if (cancelInputAfterFocusLoss)
                        {
                            // NOTE: This happens after canceling editing by closing the input
                            // and reopen the state. Sadly there doesn't appear to be a simple fix for this.
                        }

                        
                        if (ImGui.IsItemDeactivated() )
                        {
                            //Log.Debug(" is item deactivated #" + CurrentTabIndex);
                            SetState(InputStates.Inactive);
                            ImGui.SetKeyboardFocusHere(); // Clear focus so next time value will be completely selected
                            if (double.IsNaN(_editValue))
                                _editValue = _startValue;
                        }

                        _editValue = Evaluate(_jogDialText);
                        break;
                }
                

                value = _editValue;
                if (_state == InputStates.Inactive)
                {
                    return InputEditStateFlags.Finished;
                }

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
                SetState(InputStates.Dialing);
            }

            return InputEditStateFlags.Nothing;
        }

        private static void SetState(InputStates newState)
        {
            switch (newState)
            {
                case InputStates.Inactive:
                {
                    _activeJogDialId = 0;
                    break;
                }

                case InputStates.Dialing:
                    _center = ImGui.GetMousePos();
                    _timeOpened = ImGui.GetTime();
                    break;

                case InputStates.StartedTextInput:
                    break;

                case InputStates.TextInput:
                    break;
            }

            _state = newState;
        }

        public static int CurrentTabIndex = 0;
        public static int LastMaxTabIndex;
        public static int TabFocusIndex = -1; // if not -1 tries to set keyboard focus to input field.  

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
                                                        ImGui.GetItemRectMin() + new Vector2((float)orgCenter + alignment + 1, itemSize.Y),
                                                        ValueIndicatorColor);

                // Indicate overflow
                if (value < min)
                {
                    var triangleCenter = new Vector2(itemPos.X + 5, itemPos.Y + itemSize.Y - 5);
                    ImGui.GetWindowDrawList().AddTriangleFilled(
                                                                triangleCenter + new Vector2(-3, 0),
                                                                triangleCenter + new Vector2(2, -4),
                                                                triangleCenter + new Vector2(2, 4),
                                                                new Color(1, 1, 1, 0.2f)
                                                               );
                }
                else if (value > max)
                {
                    var triangleCenter = new Vector2(itemPos.X + itemSize.X - 3, itemPos.Y + itemSize.Y - 5);
                    ImGui.GetWindowDrawList().AddTriangleFilled(
                                                                triangleCenter + new Vector2(-2, -4),
                                                                triangleCenter + new Vector2(3, 0),
                                                                triangleCenter + new Vector2(-2, 4),
                                                                new Color(1, 1, 1, 0.2f)
                                                               );
                }
            }
        }

        private static readonly Color ValueIndicatorColor = new Color(1, 1, 1, 0.06f);

        private enum InputStates
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
        private static InputStates _state = InputStates.Inactive;

        private static string _numberFormat = "{0:0.000}";
        private static double _timeOpened;

        /// <summary>
        /// Draws a range of virtual slider overlays
        /// </summary>
        private static class SliderLadder
        {
            private class RangeDef
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

            private static RangeDef _lockedRange;

            public static void Draw(ImGuiIOPtr io, double min, double max, float scale, float timeSinceVisible, bool clamp)
            {
                var foreground = ImGui.GetForegroundDrawList();
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (timeSinceVisible < 0.2)
                    _lockedRange = null;

                var pLast = io.MousePos - io.MouseDelta - _center;
                var pNow = io.MousePos - _center;
                if (timeSinceVisible < 0.032f)
                {
                    _lastStepPosX = pNow.X;
                }

                float activeScaleFactor = 0;

                foreach (var range in Ranges)
                {
                    var isActiveRange = range == _lockedRange
                                        || (_lockedRange == null && pNow.Y > range.YMin && pNow.Y < range.YMax);
                    var opacity = (timeSinceVisible * 4 - range.FadeInDelay / 4).Clamp(0, 1);

                    var isCenterRange = Math.Abs(range.ScaleFactor - 1) < 0.001f;
                    if (isActiveRange)
                    {
                        activeScaleFactor = range.ScaleFactor;
                        if (_lockedRange == null && Math.Abs(ImGui.GetMouseDragDelta().X) > 30)
                        {
                            _lockedRange = range;
                        }
                    }

                    if (!isCenterRange)
                    {
                        var centerColor = (isActiveRange ? RangeActiveColor : RangeCenterColor) * opacity;

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

                if (clamp)
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
        /// This is a horrible attempt to work around imguis current limitation that button elements can't have a tab focus
        /// </summary>
        public static void StartNextFrame()
        {
            LastMaxTabIndex = CurrentTabIndex;
            CurrentTabIndex = 0;
            
            if (TabFocusIndex > LastMaxTabIndex)
            {
                Log.Debug("fixing tab overflow");
                TabFocusIndex = -1;
            }
        }
    }
}