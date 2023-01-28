using System;
using System.Numerics;
using ImGuiNET;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Styling
{
    /// <summary>
    /// A set of custom widgets that allow to quickly draw dialogs with a more "traditional" labels on the left side of input fields.
    /// It also provides a bunch of helper methods for minimal layout control. 
    /// </summary>
    public static class FormInputs
    {
        public static bool AddInt(string label,
                                        ref int value,
                                        int min = int.MinValue,
                                        int max = int.MaxValue,
                                        float scale = 1,
                                        string tooltip = null,
                                        int defaultValue = NotADefaultValue)
        {
            DrawInputLabel(label);

            ImGui.PushID(label);

            var hasReset = defaultValue != NotADefaultValue;

            var size = GetAvailableParamSize(tooltip, hasReset);
            var result = SingleValueEdit.Draw(ref value, size, min, max, true, scale);
            ImGui.PopID();

            AppendTooltip(tooltip);
            if (AppendResetButton(hasReset))
            {
                value = defaultValue;
                result |= InputEditStateFlags.ModifiedAndFinished;
            }

            var modified = (result & InputEditStateFlags.Modified) != InputEditStateFlags.Nothing;
            return modified;
        }

        private const float DefaultFadeAlpha = 0.7f;

        public static bool AddFloat(string label, 
                                          ref float value, 
                                          float min = float.NegativeInfinity, 
                                          float max = float.PositiveInfinity,
                                          float scale = 0.01f, 
                                          bool clamp = false, 
                                          string tooltip = null,
                                          float defaultValue = float.NaN)
        {
            var hasReset = !float.IsNaN(defaultValue);
            var isDefault = hasReset && Math.Abs(value - defaultValue) < 0.0001f;
            if (isDefault)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha,DefaultFadeAlpha * ImGui.GetStyle().Alpha);
            }
            
            DrawInputLabel(label);
            var size = GetAvailableParamSize(tooltip, hasReset);
            
            ImGui.PushID(label);
            var result = SingleValueEdit.Draw(ref value, size, min, max, clamp, scale);
            ImGui.PopID();

            AppendTooltip(tooltip);
            if (AppendResetButton(hasReset && !isDefault))
            {
                value = defaultValue;
                result |= InputEditStateFlags.ModifiedAndFinished;
            }
            
            if (isDefault)
            {
                ImGui.PopStyleVar();
            }
            
            var modified = (result & InputEditStateFlags.Modified) != InputEditStateFlags.Nothing;
            return modified;
        }

        public static bool AddEnumDropdown<T>(ref int index, string label)
        {
            DrawInputLabel(label);
            var size = new Vector2(150 * T3Ui.UiScaleFactor, ImGui.GetFrameHeight());

            Type enumType = typeof(T);
            var values = Enum.GetValues(enumType);

            var valueNames = new string[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                var v = values.GetValue(i);
                valueNames[i] = v != null
                                    ? Enum.GetName(typeof(T), v)
                                    : "?? undefined";
            }

            ImGui.SetNextItemWidth(size.X);
            // FIXME: using only "##dropdown" did not allow for multiple combos (see for example renderSequenceWindow.cs)
            // so we add the type and label here - but this is only a temporary hack...
            var modified = ImGui.Combo($"##dropDown{enumType}{label}", ref index, valueNames, valueNames.Length, valueNames.Length);
            return modified;
        }
        
        public static bool AddEnumDropdown<T>(ref T selectedValue, string label) where T : struct, Enum, IConvertible, IFormattable
        {
            DrawInputLabel(label);
            var size = new Vector2(150 * T3Ui.UiScaleFactor, ImGui.GetFrameHeight());
            
            var names = Enum.GetNames<T>();
            var index = 0;
            var selectedIndex = 0;
            
            foreach (var n in names)
            {
                if (n == selectedValue.ToString())
                    selectedIndex = index;
                
                index++;
            }

            ImGui.SetNextItemWidth(size.X);
            // FIXME: using only "##dropdown" did not allow for multiple combos (see for example renderSequenceWindow.cs)
            // so we add the type and label here - but this is only a temporary hack...
            var modified = ImGui.Combo($"##dropDown{label}", ref selectedIndex, names, names.Length, names.Length);
            if (modified)
            {
                selectedValue = Enum.GetValues<T>()[selectedIndex];
            }

            return modified;
        }        
        
        public static bool AddSegmentedButton<T>(ref T selectedValue, string label) where T : struct, Enum
        {
            DrawInputLabel(label);
            
            var modified = false;
            var selectedValueString = selectedValue.ToString();
            var isFirst = true;
            foreach(var value in Enum.GetValues<T>())
            {
                var name = Enum.GetName(value);
                if (!isFirst)
                {
                    ImGui.SameLine();
                }
                
                var isSelected = selectedValueString == value.ToString();
                var clicked = DrawSelectButton(name, isSelected);
                
                if(clicked)
                {
                    modified = true;
                    selectedValue = value;
                }

                isFirst = false;
            }
            
            return modified;
        }

        private static bool DrawSelectButton(string name, bool isSelected)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, isSelected ? T3Style.Colors.ButtonActive.Rgba: T3Style.Colors.ButtonHover.Rgba);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, isSelected ? T3Style.Colors.ButtonActive.Rgba: T3Style.Colors.ButtonHover.Rgba);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, T3Style.Colors.ButtonActive.Rgba);

            var clicked = ImGui.Button(name);
            //ImGui.SameLine();
            ImGui.PopStyleColor(3);
            return clicked;
        }

        /// <summary>
        /// Draws string input or file picker. 
        /// </summary>
        public static bool AddStringInput(string label,
                                           ref string value,
                                           string placeHolder = null,
                                           string warning = null)
        {
            DrawInputLabel(label);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 50);

            var wasNull = value == null;
            if (wasNull)
                value = string.Empty;

            var modified = ImGui.InputText("##" + label, ref value, 1000);
            if (!modified && wasNull)
                value = null;

            if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(placeHolder))
            {
                var drawList = ImGui.GetWindowDrawList();
                var minPos = ImGui.GetItemRectMin();
                var maxPos = ImGui.GetItemRectMax();
                drawList.PushClipRect(minPos, maxPos);
                drawList.AddText(minPos + new Vector2(8, 3), Color.White.Fade(0.25f), placeHolder);
                drawList.PopClipRect();
            }

            DrawWarningBelowField(warning);

            return modified;
        }

        /// <summary>
        /// Draws string input or file picker. 
        /// </summary>
        public static bool AddFilePicker(string label,
                                      ref string value,
                                      string placeHolder = null,
                                      string warning = null,
                                      FileOperations.FilePickerTypes showFilePicker = FileOperations.FilePickerTypes.None)
        {
            DrawInputLabel(label);
            var cursorX = ImGui.GetCursorPosX();
            var isFilePickerVisible = showFilePicker != FileOperations.FilePickerTypes.None;
            float spaceForFilePicker = isFilePickerVisible ? 30 : 0;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 50 - spaceForFilePicker);

            var wasNull = value == null;
            if (wasNull)
                value = string.Empty;

            var modified = ImGui.InputText("##" + label, ref value, 1000);
            if (!modified && wasNull)
                value = null;

            if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(placeHolder))
            {
                var drawList = ImGui.GetWindowDrawList();
                var minPos = ImGui.GetItemRectMin();
                var maxPos = ImGui.GetItemRectMax();
                drawList.PushClipRect(minPos, maxPos);
                drawList.AddText(minPos + new Vector2(8, 3), Color.White.Fade(0.25f), placeHolder);
                drawList.PopClipRect();
            }

            if (isFilePickerVisible)
            {
                modified |= FileOperations.DrawFileSelector(showFilePicker, ref value);
            }

            if (!string.IsNullOrEmpty(warning))
            {
                ImGui.SetCursorPosX(cursorX);
                ImGui.PushFont(Fonts.FontSmall);
                ImGui.PushStyleColor(ImGuiCol.Text, Color.Red.Rgba);
                ImGui.TextUnformatted(warning);
                ImGui.PopStyleColor();
                ImGui.PopFont();
            }

            return modified;
        }

        public static bool AddCheckBox(
            string label, 
            ref bool value, 
            string tooltip = null,
            bool? defaultValue = null)
        {
            var hasDefault = defaultValue != null;
            var isDefault = hasDefault && value == (bool)defaultValue;
            
            if (isDefault)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, DefaultFadeAlpha);
            }
            
            ImGui.SetCursorPosX(MathF.Max(LeftParameterPadding, 0) + 15);
            var modified = ImGui.Checkbox(label, ref value);
            
            
            AppendTooltip(tooltip);
            if (isDefault)
            {
                ImGui.PopStyleVar();
            }
            
            if (AppendResetButton(hasDefault && !isDefault))
            {
                value = defaultValue ?? false;
                modified = true;
            }
            return modified;
        }

        public static void AddHint(string label)
        {
            if (string.IsNullOrEmpty(label))
                return;
            
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10f);
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10,20));
            
            ImGui.Indent(13);
            AddIcon(Icon.Hint);

            ImGui.SameLine();
            ImGui.TextUnformatted(label);
            ImGui.Indent(-13);
            ImGui.PopStyleVar(2);
        }

        public static void AddVerticalSpace(float size = 10)
        {
            ImGui.Dummy(new Vector2(1, size * T3Ui.UiScaleFactor));
        }

        #region layout helpers
        public static void SetIndent(float newIndent)
        {
            _paramIndent = newIndent;
        }

        public static void ResetIndent()
        {
            _paramIndent = DefaultParameterIndent;
        }

        public static void ApplyIndent()
        {
            ImGui.SetCursorPosX(LeftParameterPadding + ParameterSpacing);
        }
        
        public static void DrawInputLabel(string label)
        {
            var labelSize = ImGui.CalcTextSize(label);
            var p = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(MathF.Max(LeftParameterPadding - labelSize.X, 0) + 10);
            ImGui.AlignTextToFramePadding();

            ImGui.TextUnformatted(label);
            ImGui.SetCursorPos(p);

            ImGui.SameLine();
            ImGui.SetCursorPosX(LeftParameterPadding + ParameterSpacing);
        }

        private static void DrawWarningBelowField(string warning)
        {
            if (string.IsNullOrEmpty(warning))
                return;

            ImGui.SetCursorPosX(MathF.Max(LeftParameterPadding, 0) + 10);
            ImGui.PushFont(Fonts.FontSmall);
            ImGui.PushStyleColor(ImGuiCol.Text, Color.Red.Rgba);
            ImGui.TextUnformatted(warning);
            ImGui.PopStyleColor();
            ImGui.PopFont();
        }
        #endregion

        #region internal helpers
        private static Vector2 GetAvailableParamSize(string tooltip, bool hasReset)
        {
            var toolWidth = 30 * T3Ui.UiScaleFactor;
            var sizeForResetToDefault = hasReset ? toolWidth : 0;
            var sizeForTooltip = !string.IsNullOrEmpty(tooltip) ? toolWidth : 0;
            var vector2 = new Vector2(150 * T3Ui.UiScaleFactor
                                      - sizeForResetToDefault
                                      - sizeForTooltip, ImGui.GetFrameHeight());
            return vector2;
        }

        private static void AppendTooltip(string tooltip)
        {
            if (string.IsNullOrEmpty(tooltip))
                return;

            ImGui.SameLine();
            //CustomComponents.IconButton(Icon.Help, "##tooltip", Vector2.One * ImGui.GetFrameHeight());
            
            ImGui.PushFont(Icons.IconFont);
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f, 0.5f));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
            
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(" "+(char)Icon.Help);

            ImGui.PopStyleVar(2);
            ImGui.PopFont();
            
            if (!ImGui.IsItemHovered())
                return;
            
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(300);
            ImGui.TextUnformatted(tooltip);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }

        private static bool AppendResetButton(bool hasReset)
        {
            if (!hasReset)
                return false;

            ImGui.SameLine();
            return CustomComponents.IconButton(Icon.Revert, "##revert", Vector2.One * ImGui.GetFrameHeight());
        }

        private static void AddIcon(Icon icon)
        {
            ImGui.PushFont(Icons.IconFont);
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f, 0.5f));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);

            ImGui.TextUnformatted("" + (char)(icon));

            ImGui.PopFont();
            ImGui.PopStyleVar(2);
        }
        #endregion

        private const int NotADefaultValue = Int32.MinValue;

        private const float DefaultParameterIndent = 170;
        private static float _paramIndent = DefaultParameterIndent;
        private static float LeftParameterPadding => _paramIndent * T3Ui.UiScaleFactor;
        public static float ParameterSpacing => 20 * T3Ui.UiScaleFactor;
    }
}