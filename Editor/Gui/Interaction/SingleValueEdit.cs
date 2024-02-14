using System;
using System.Data;
using System.Globalization;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Utils;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using Vector2 = System.Numerics.Vector2;

namespace T3.Editor.Gui.Interaction
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
                                               float scale = 1f,
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

        private static int _editInteractionCounter =0;
        
        public static InputEditStateFlags Draw(ref double value,
                                               Vector2 size,
                                               double min = double.NegativeInfinity,
                                               double max = double.PositiveInfinity,
                                               bool clamp = false,
                                               float scale = 1,
                                               string format = "{0:0.000}")
        {
            _currentTabIndex++;
            var componentId = ImGui.GetID("valueEdit");

            var shouldFocus = _currentTabIndex == _tabFocusIndex;
            if (shouldFocus)
            {
                //Log.Debug("  ShouldFocus for index " + TabFocusIndex  +  "  state " + _state );
                SetState(InputStates.TextInput);
                _activeJogDialId = componentId;
                _jogDialText = FormatValueForButton(ref value);
            }

            var io = ImGui.GetIO();

            _numberFormat = format;
            if (componentId == _activeJogDialId)
            {
                switch (_state)
                {
                    case InputStates.Dialing:
                        ImGui.PushStyleColor(ImGuiCol.Button, UiColors.BackgroundActive.Rgba);
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, UiColors.BackgroundActive.Rgba);
                        ImGui.PushStyleColor(ImGuiCol.ButtonActive, UiColors.BackgroundActive.Rgba);
                        DrawButtonWithDynamicLabel(FormatValueForButton(ref _editValue), ref size);
                        DrawValueRangeIndicator(value, min, max);
                        ImGui.PopStyleColor(3);

                        if (ImGui.IsMouseReleased(0))
                        {
                            var wasClick = ImGui.GetIO().MouseDragMaxDistanceSqr[0] < UserSettings.Config.ClickThreshold;
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
                        var restarted = (float)(ImGui.GetTime() - _timeOpened) < 0.1f;
                        DrawValueEditMethod(ref _editValue, restarted,_center, min, max, clamp, scale);

                        break;

                    case InputStates.StartedTextInput:
                        _editInteractionCounter++;
                        ImGui.SetKeyboardFocusHere();
                        SetState(InputStates.TextInput);
                        goto case InputStates.TextInput;

                    case InputStates.TextInput:
                        ImGui.PushStyleColor(ImGuiCol.Text, double.IsNaN(_editValue)
                                                                ? UiColors.StatusError.Rgba
                                                                : UiColors.ForegroundFull);
                        ImGui.SetNextItemWidth(size.X);
                        ImGui.InputText("##dialInput" + _editInteractionCounter, ref _jogDialText, 20);

                        // Keep Focusing until Tab-Key released
                        if (shouldFocus)
                        {
                            ImGui.SetKeyboardFocusHere(-1);
                            if (ImGui.IsKeyReleased((ImGuiKey)Key.Tab))
                            {
                                _tabFocusIndex = -1;
                            }
                        }

                        ImGui.PopStyleColor();
                        var completedAfterTabbing = false;
                        if (ImGui.IsKeyPressed((ImGuiKey)Key.Tab) && _tabFocusIndex == -1)
                        {
                            _tabFocusIndex = _currentTabIndex + (ImGui.GetIO().KeyShift ? -1 : 1);
                            completedAfterTabbing = true;
                        }

                        var cancelInputAfterFocusLoss = !shouldFocus && !ImGui.IsItemActive();
                        if (cancelInputAfterFocusLoss)
                        {
                            // NOTE: This happens after canceling editing by closing the input
                            // and reopen the state. Sadly there doesn't appear to be a simple fix for this.
                        }

                        if (completedAfterTabbing || ImGui.IsKeyPressed((ImGuiKey)Key.Esc) || ImGui.IsItemDeactivated() || !ImGui.IsWindowFocused())
                        {
                            SetState(InputStates.Inactive);
                            if (double.IsNaN(_editValue))
                                _editValue = _startValue;
                        }

                        var valid = Evaluate(_jogDialText, ref _editValue);

                        // If the value is invalid, just revert it to what it was previously
                        if (!valid)
                        {
                            _editValue = _startValue;
                            _jogDialText = value.ToString();
                        }
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
                _activeJogDialId = componentId;
                _editValue = value;
                _startValue = value;
                _jogDialText = FormatValueForButton(ref value);
                SetState(InputStates.Dialing);
            }
            else
            {
                var isHovered = ImGui.IsItemHovered();

                if (_state == InputStates.Inactive)
                {
                    var isHoveredComponent = _activeHoverComponentId == componentId;
                    if (isHoveredComponent)
                    {
                        if (isHovered && (!UserSettings.Config.MouseWheelEditsNeedCtrlKey || io.KeyCtrl))
                        {
                            T3Ui.MouseWheelFieldHovered = true;
                            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
                            var dl = ImGui.GetForegroundDrawList();
                            dl.AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), UiColors.StatusActivated);

                            var wheel = io.MouseWheel;
                            if (wheel == 0)
                                return InputEditStateFlags.Nothing;

                            var factor = 1f;
                            if (io.KeyShift)
                            {
                                factor = 0.01f;
                            }
                            else if (io.KeyAlt)
                            {
                                factor = 10f;
                            }

                            value += wheel * scale * 10  * factor;
                            _hoveredComponentModifiedByWheel = true;
                            return InputEditStateFlags.Modified;
                        }

                        var didModify = _hoveredComponentModifiedByWheel;
                        _hoveredComponentModifiedByWheel = false;

                        return didModify
                                   ? InputEditStateFlags.ModifiedAndFinished
                                   : InputEditStateFlags.Nothing;
                    }

                    if (isHovered)
                    {
                        T3Ui.MouseWheelFieldHovered = true;
                        _hoveredComponentModifiedByWheel = false;
                        _activeHoverComponentId = componentId;
                        return InputEditStateFlags.Started;
                    }
                }
            }

            return InputEditStateFlags.Nothing;
        }

        public static void DrawValueEditMethod(ref double editValue, bool restarted, Vector2 center, double min, double max, bool clamp, float scale)
        {
            switch (UserSettings.Config.ValueEditMethod)
            {
                case UserSettings.ValueEditMethods.InfinitySlider:
                    InfinitySliderOverlay.Draw(ref editValue, restarted, center, min, max, scale, clamp);
                    break;
                case UserSettings.ValueEditMethods.RadialSlider:
                    RadialSliderOverlay.Draw(ref editValue, restarted, center, min, max, scale, clamp);
                    break;
                case UserSettings.ValueEditMethods.JogDial:
                    JogDialOverlay.Draw(ref editValue, restarted, center, min, max, scale, clamp);
                    break;
                case UserSettings.ValueEditMethods.ValueLadder:
                default:
                    SliderLadder.Draw(ref editValue, min, max, scale, (float)(ImGui.GetTime() - _timeOpened), clamp, center);
                    break;
            }
        }

        private static bool _hoveredComponentModifiedByWheel;

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

        private static int _currentTabIndex;
        private static int _lastMaxTabIndex;
        private static int _tabFocusIndex = -1; // if not -1 tries to set keyboard focus to input field.  

        private static bool Evaluate(string expression, ref double editValue)
        {
            try
            {
                var table = new DataTable();
                table.Columns.Add("expression", typeof(string), expression);
                var row = table.NewRow();
                table.Rows.Add(row);
                var newValue = double.Parse((string)row["expression"]);
                
                if (double.IsNaN(newValue) || double.IsInfinity(newValue))
                    return false;
                
                editValue = newValue;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string FormatValueForButton(ref double value)
        {
            // Don't use rounding for integers
            try
            {
                return (_numberFormat == "{0:0}")
                           ? "" + (int)value
                           : string.Format(_numberFormat, value);
            }
            catch (FormatException)
            {
                Log.Warning($"Invalid value format '{_numberFormat}'");
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// A horrible ImGui work around to have button that stays active while its label changes.  
        /// </summary>
        private static void DrawButtonWithDynamicLabel(string label, ref Vector2 size)
        {
            var color1 = ImGuiCol.Text.GetStyleColor().Fade(ImGui.GetStyle().Alpha);
            var keepPos = ImGui.GetCursorScreenPos();
            ImGui.Button("##dial", size);
            if (string.IsNullOrEmpty(label))
                return;
            
            ImGui.GetWindowDrawList().AddText(keepPos + new Vector2(4, 4), color1, label);
        }

        private static void DrawValueRangeIndicator(double value, double min, double max)
        {
            if (!double.IsInfinity(min) || !double.IsInfinity(max))
            {
                var itemSize = ImGui.GetItemRectSize();
                var itemPos = ImGui.GetItemRectMin();

                var center = 0.0;
                if (Math.Abs(min) > 0.001f)
                {
                    center = MathUtils.RemapAndClamp(0, min, max, 0, itemSize.X);
                    // center = MathUtils.RemapAndClamp((min + max) * 0.5, min, max, 0, itemSize.X);
                }

                var end = MathUtils.RemapAndClamp(value, min, max, 0, itemSize.X);
                var orgCenter = center;

                if (center > end)
                {
                    (center, end) = (end, center);
                }

                var p1 = itemPos + new Vector2((float)center, 0);
                var p2 = itemPos + new Vector2((float)end, itemSize.Y);
                ImGui.GetWindowDrawList().AddRectFilled(p1, p2, _valueIndicatorColor);

                // Indicate center
                var alignment = center < orgCenter ? -1 : 0;
                ImGui.GetWindowDrawList().AddRectFilled(
                                                        ImGui.GetItemRectMin() + new Vector2((float)orgCenter + alignment, 0),
                                                        ImGui.GetItemRectMin() + new Vector2((float)orgCenter + alignment + 1, itemSize.Y),
                                                        _valueIndicatorColor);

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

        private static readonly Color _valueIndicatorColor = new(1, 1, 1, 0.06f);

        private enum InputStates
        {
            Inactive,
            Dialing,
            StartedTextInput,
            TextInput,
        }

        private static uint _activeJogDialId;
        private static uint _activeHoverComponentId;
        private static Vector2 _center;
        private static double _editValue;
        private static double _startValue;
        private static string _jogDialText = "";
        private static InputStates _state = InputStates.Inactive;

        private static string _numberFormat = "{0:0.000}";
        private static double _timeOpened;
        private const double DefaultValue = 0.0;

        /// <summary>
        /// This is a horrible attempt to work around imguis current limitation that button elements can't have a tab focus
        /// </summary>
        public static void StartNextFrame()
        {
            _lastMaxTabIndex = _currentTabIndex;
            _currentTabIndex = 0;

            if (_tabFocusIndex > _lastMaxTabIndex)
            {
                Log.Debug("fixing tab overflow");
                _tabFocusIndex = -1;
            }
        }
    }
}